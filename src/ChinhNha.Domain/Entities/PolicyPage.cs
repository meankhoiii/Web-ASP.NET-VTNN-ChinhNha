namespace ChinhNha.Domain.Entities;

public class PolicyPage : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    
    public DateTime? UpdatedAt { get; set; }
}
