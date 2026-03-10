namespace ChinhNha.Domain.Entities;

public class ContactMessage : BaseEntity
{
    public string? UserId { get; set; }
    public AppUser? User { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    
    public bool IsRead { get; set; } = false;
    public DateTime? RepliedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
