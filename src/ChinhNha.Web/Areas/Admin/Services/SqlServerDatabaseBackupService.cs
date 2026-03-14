using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace ChinhNha.Web.Areas.Admin.Services;

public class SqlServerDatabaseBackupService : IDatabaseBackupService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SqlServerDatabaseBackupService> _logger;

    public SqlServerDatabaseBackupService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<SqlServerDatabaseBackupService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DatabaseBackupItem>> GetBackupsAsync()
    {
        var candidates = await GetCandidateDirectoriesAsync(CancellationToken.None);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new List<DatabaseBackupItem>();

        foreach (var directory in candidates)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                foreach (var path in Directory.GetFiles(directory, "*.bak", SearchOption.TopDirectoryOnly))
                {
                    if (!seen.Add(path))
                    {
                        continue;
                    }

                    var info = new FileInfo(path);
                    files.Add(new DatabaseBackupItem(
                        FileName: info.Name,
                        CreatedAt: info.LastWriteTime,
                        FileSizeBytes: info.Length,
                        IsPreRestoreBackup: info.Name.Contains("prerestore", StringComparison.OrdinalIgnoreCase)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể đọc danh sách backup từ thư mục {Directory}", directory);
            }
        }

        files = files
            .OrderByDescending(item => item.CreatedAt)
            .ToList();

        return await Task.FromResult(files);
    }

    public async Task<string> CreateBackupAsync(string reason = "manual", CancellationToken cancellationToken = default)
    {
        var databaseName = GetDatabaseName();
        var suffix = string.IsNullOrWhiteSpace(reason) ? "manual" : reason.Trim().ToLowerInvariant();
        var backupFileName = $"{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}_{suffix}.bak";
        var attempts = new List<string>();

        var candidates = await GetCandidateDirectoriesAsync(cancellationToken);
        foreach (var backupDirectory in candidates)
        {
            try
            {
                Directory.CreateDirectory(backupDirectory);
                var backupPath = Path.Combine(backupDirectory, backupFileName);
                await ExecuteBackupAsync(backupPath, cancellationToken);
                return backupFileName;
            }
            catch (Exception ex)
            {
                attempts.Add($"{backupDirectory}: {ex.Message}");
                _logger.LogWarning(ex, "Tạo backup thất bại tại {Directory}, thử thư mục kế tiếp.", backupDirectory);
            }
        }

        throw new InvalidOperationException(
            "Không thể tạo backup ở bất kỳ thư mục nào khả dụng. " + string.Join(" | ", attempts));
    }

    public async Task<DatabaseRestoreResult> RestoreAsync(string backupFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupFileName))
        {
            throw new ArgumentException("Tên file backup không hợp lệ.", nameof(backupFileName));
        }

        var safeFileName = Path.GetFileName(backupFileName);
        var candidates = await GetCandidateDirectoriesAsync(cancellationToken);
        var restoreSourcePath = candidates
            .Select(directory => Path.Combine(directory, safeFileName))
            .FirstOrDefault(File.Exists);

        if (string.IsNullOrWhiteSpace(restoreSourcePath))
        {
            throw new FileNotFoundException("Không tìm thấy file backup cần khôi phục.", safeFileName);
        }

        var databaseName = GetDatabaseName();
        var backupDirectory = Path.GetDirectoryName(restoreSourcePath) ?? EnsureBackupDirectory();
        var preRestoreBackupFileName = $"{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}_prerestore.bak";
        var preRestoreBackupPath = Path.Combine(backupDirectory, preRestoreBackupFileName);

        await ExecuteBackupAsync(preRestoreBackupPath, cancellationToken);

        var restoreSql = $@"
ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{databaseName}] FROM DISK = N'{EscapeSqlLiteral(restoreSourcePath)}' WITH REPLACE, RECOVERY;
ALTER DATABASE [{databaseName}] SET MULTI_USER;";

        try
        {
            await ExecuteOnMasterAsync(restoreSql, 0, cancellationToken);
        }
        catch
        {
            try
            {
                await ExecuteOnMasterAsync($"ALTER DATABASE [{databaseName}] SET MULTI_USER;", 0, cancellationToken);
            }
            catch (Exception unlockEx)
            {
                _logger.LogError(unlockEx, "Không thể chuyển database về MULTI_USER sau khi restore lỗi.");
            }

            throw;
        }

        return new DatabaseRestoreResult(
            RestoredFromFile: safeFileName,
            PreRestoreBackupFile: preRestoreBackupFileName,
            RestoredAt: DateTime.Now);
    }

    public string GetBackupDirectory()
    {
        var configured = GetConfiguredBackupDirectory();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return EnsureBackupDirectory();
    }

    private async Task<List<string>> GetCandidateDirectoriesAsync(CancellationToken cancellationToken)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddIfValid(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var fullPath = Path.GetFullPath(path);
            if (seen.Add(fullPath))
            {
                candidates.Add(fullPath);
            }
        }

        AddIfValid(GetConfiguredBackupDirectory());
        AddIfValid(await GetSqlDefaultBackupDirectoryAsync(cancellationToken));
        AddIfValid(EnsureBackupDirectory());

        return candidates;
    }

    private async Task ExecuteBackupAsync(string backupPath, CancellationToken cancellationToken)
    {
        var databaseName = GetDatabaseName();

        try
        {
            await ExecuteOnMasterAsync(
                BuildBackupSql(databaseName, backupPath, useCompression: true),
                0,
                cancellationToken);
        }
        catch (SqlException ex) when (IsCompressionNotSupported(ex))
        {
            _logger.LogWarning(
                ex,
                "SQL Server edition không hỗ trợ BACKUP COMPRESSION. Thử lại backup không nén tại {BackupPath}",
                backupPath);

            await ExecuteOnMasterAsync(
                BuildBackupSql(databaseName, backupPath, useCompression: false),
                0,
                cancellationToken);
        }
    }

    private async Task ExecuteOnMasterAsync(string sql, int commandTimeoutSeconds, CancellationToken cancellationToken)
    {
        var connectionString = GetMasterConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = commandTimeoutSeconds;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GetDatabaseName()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new InvalidOperationException("Không xác định được tên database từ connection string.");
        }

        return builder.InitialCatalog;
    }

    private string GetMasterConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.");

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        return builder.ConnectionString;
    }

    private string EnsureBackupDirectory()
    {
        var backupDirectory = Path.Combine(_environment.ContentRootPath, "backups", "database");
        Directory.CreateDirectory(backupDirectory);
        return backupDirectory;
    }

    private string? GetConfiguredBackupDirectory()
    {
        return _configuration["DatabaseBackup:Directory"];
    }

    private async Task<string?> GetSqlDefaultBackupDirectoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            const string sql = "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'));";
            return await ExecuteScalarOnMasterAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lấy InstanceDefaultBackupPath từ SQL Server.");
            return null;
        }
    }

    private async Task<string?> ExecuteScalarOnMasterAsync(string sql, CancellationToken cancellationToken)
    {
        var connectionString = GetMasterConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value?.ToString();
    }

    private static string EscapeSqlLiteral(string value)
    {
        return value.Replace("'", "''");
    }

    private static bool IsCompressionNotSupported(SqlException ex)
    {
        return ex.Message.Contains("COMPRESSION is not supported", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("not supported on Express Edition", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildBackupSql(string databaseName, string backupPath, bool useCompression)
    {
        var compressionClause = useCompression ? ", COMPRESSION" : string.Empty;

        return $@"
BACKUP DATABASE [{databaseName}]
TO DISK = N'{EscapeSqlLiteral(backupPath)}'
WITH COPY_ONLY, INIT{compressionClause}, CHECKSUM;";
    }
}
