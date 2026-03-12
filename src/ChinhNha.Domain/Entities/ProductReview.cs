using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Entities;

public class ProductReview : BaseEntity
{
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    
    public int Rating { get; set; } // 1-5 stars
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    public int HelpfulCount { get; set; } = 0;
    public int UnhelpfulCount { get; set; } = 0;
    
    public bool IsVerifiedPurchase { get; set; } = false;
    public bool IsApproved { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Product Product { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
