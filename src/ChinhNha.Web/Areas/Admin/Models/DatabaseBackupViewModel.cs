using ChinhNha.Web.Areas.Admin.Services;

namespace ChinhNha.Web.Areas.Admin.Models;

public class DatabaseBackupViewModel
{
    public string BackupDirectory { get; set; } = string.Empty;
    public List<DatabaseBackupItem> Backups { get; set; } = new();
}
