using Microsoft.Data.SqlClient;

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
        var backupDirectory = EnsureBackupDirectory();

        var files = Directory
            .GetFiles(backupDirectory, "*.bak", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTime)
            .Select(info => new DatabaseBackupItem(
                FileName: info.Name,
                CreatedAt: info.LastWriteTime,
                FileSizeBytes: info.Length,
                IsPreRestoreBackup: info.Name.Contains("prerestore", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return await Task.FromResult(files);
    }

    public async Task<string> CreateBackupAsync(string reason = "manual", CancellationToken cancellationToken = default)
    {
        var databaseName = GetDatabaseName();
        var backupDirectory = EnsureBackupDirectory();
        var suffix = string.IsNullOrWhiteSpace(reason) ? "manual" : reason.Trim().ToLowerInvariant();
        var backupFileName = $"{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}_{suffix}.bak";
        var backupPath = Path.Combine(backupDirectory, backupFileName);

        await ExecuteBackupAsync(backupPath, cancellationToken);
        return backupFileName;
    }

    public async Task<DatabaseRestoreResult> RestoreAsync(string backupFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupFileName))
        {
            throw new ArgumentException("Tên file backup không hợp lệ.", nameof(backupFileName));
        }

        var backupDirectory = EnsureBackupDirectory();
        var safeFileName = Path.GetFileName(backupFileName);
        var restoreSourcePath = Path.Combine(backupDirectory, safeFileName);

        if (!File.Exists(restoreSourcePath))
        {
            throw new FileNotFoundException("Không tìm thấy file backup cần khôi phục.", safeFileName);
        }

        var databaseName = GetDatabaseName();
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
        return EnsureBackupDirectory();
    }

    private async Task ExecuteBackupAsync(string backupPath, CancellationToken cancellationToken)
    {
        var databaseName = GetDatabaseName();
        var backupSql = $@"
BACKUP DATABASE [{databaseName}]
TO DISK = N'{EscapeSqlLiteral(backupPath)}'
WITH COPY_ONLY, INIT, COMPRESSION, CHECKSUM;";

        await ExecuteOnMasterAsync(backupSql, 0, cancellationToken);
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

    private static string EscapeSqlLiteral(string value)
    {
        return value.Replace("'", "''");
    }
}
