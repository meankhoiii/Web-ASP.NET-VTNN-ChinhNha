namespace ChinhNha.Domain.Entities;

/// <summary>
/// Store customer's wishlist/favorite products
/// </summary>
public class Wishlist : BaseEntity
{
    /// <summary>User who owns this wishlist</summary>
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    /// <summary>Product in the wishlist</summary>
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Name of the wishlist (e.g., "For Home", "Birthday Gifts")</summary>
    public string? WishlistName { get; set; }

    /// <summary>Is this the default wishlist</summary>
    public bool IsDefault { get; set; } = true;

    /// <summary>Notes about why user added this product</summary>
    public string? Notes { get; set; }

    /// <summary>Priority level (1-5, 5 being highest)</summary>
    public int Priority { get; set; } = 3;

    /// <summary>Date when product was added to wishlist</summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Expected purchase date</summary>
    public DateTime? PurchasedAt { get; set; }

    /// <summary>Price when added to wishlist for price drop tracking</summary>
    public decimal? PriceWhenAdded { get; set; }
}
