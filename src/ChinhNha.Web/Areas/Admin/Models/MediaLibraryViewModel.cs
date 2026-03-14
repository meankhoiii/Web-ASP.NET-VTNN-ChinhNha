namespace ChinhNha.Web.Areas.Admin.Models;

public class MediaLibraryViewModel
{
    public string? FieldId { get; set; }
    public bool IsPicker { get; set; }
    public List<MediaFileItemViewModel> Files { get; set; } = new();
}

public class MediaFileItemViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
