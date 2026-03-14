namespace ChinhNha.Web.Areas.Admin.Services;

public interface IDatabaseBackupService
{
    Task<IReadOnlyList<DatabaseBackupItem>> GetBackupsAsync();
    Task<string> CreateBackupAsync(string reason = "manual", CancellationToken cancellationToken = default);
    Task<DatabaseRestoreResult> RestoreAsync(string backupFileName, CancellationToken cancellationToken = default);
    string GetBackupDirectory();
}

public sealed record DatabaseBackupItem(
    string FileName,
    DateTime CreatedAt,
    long FileSizeBytes,
    bool IsPreRestoreBackup);

public sealed record DatabaseRestoreResult(
    string RestoredFromFile,
    string PreRestoreBackupFile,
    DateTime RestoredAt);
