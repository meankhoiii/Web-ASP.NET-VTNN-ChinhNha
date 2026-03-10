namespace ChinhNha.Domain.Entities;

public class Banner : BaseEntity
{
    public string? Title { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    
    public string Position { get; set; } = "HomeSlider"; 
    
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
