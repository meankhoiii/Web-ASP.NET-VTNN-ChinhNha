namespace ChinhNha.Domain.Entities;

/// <summary>
/// Stores saved search filter preferences for customers
/// </summary>
public class SavedSearchFilter : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    
    public string FilterName { get; set; } = string.Empty;
    public string FiltersJson { get; set; } = string.Empty; // Store filter criteria as JSON
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public int UsageCount { get; set; }
}
