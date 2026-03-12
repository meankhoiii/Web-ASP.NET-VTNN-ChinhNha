namespace ChinhNha.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, LOGIN, etc.
    public string EntityType { get; set; } = string.Empty; // Product, Order, User, etc.
    public int? EntityId { get; set; }
    
    public string? OldValues { get; set; } // JSON serialized old values
    public string? NewValues { get; set; } // JSON serialized new values
    
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
    
    // Navigation
    public AppUser? User { get; set; }
}

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    Viewed,
    Login,
    Logout,
    Approved,
    Rejected,
    Exported,
    Imported,
    Published,
    Unpublished
}

public enum AuditEntityType
{
    User,
    Product,
    Category,
    Order,
    Payment,
    Inventory,
    Blog,
    Banner,
    Policy,
    Settings,
    ProductReview,
    Cart,
    Unknown
}
