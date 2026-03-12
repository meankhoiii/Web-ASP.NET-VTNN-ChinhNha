using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

/// <summary>Repository for wishlist management</summary>
public interface IWishlistRepository : IRepository<Wishlist>
{
    /// <summary>Get all wishlists for a user</summary>
    Task<IEnumerable<Wishlist>> GetUserWishlistsAsync(string userId);

    /// <summary>Get user's default wishlist</summary>
    Task<Wishlist?> GetDefaultWishlistAsync(string userId);

    /// <summary>Check if product is in user's wishlist</summary>
    Task<bool> IsProductInWishlistAsync(string userId, int productId);

    /// <summary>Get wishlist by ID and verify ownership</summary>
    Task<Wishlist?> GetWishlistByIdAsync(int id, string userId);

    /// <summary>Add product to wishlist</summary>
    Task<Wishlist> AddToWishlistAsync(string userId, int productId, decimal currentPrice, string? notes = null);

    /// <summary>Remove product from wishlist</summary>
    Task<bool> RemoveFromWishlistAsync(string userId, int productId);

    /// <summary>Get products with price drops</summary>
    Task<IEnumerable<Wishlist>> GetPriceDropsAsync(string userId);

    /// <summary>Count total items in user's wishlists</summary>
    Task<int> GetWishlistCountAsync(string userId);
}
