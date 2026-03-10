using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Domain.Entities;

public class Cart : BaseEntity, IAggregateRoot
{
    public string? UserId { get; set; }
    public AppUser? User { get; set; }

    public string? SessionId { get; set; } // Hỗ trợ khách vãng lai

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
