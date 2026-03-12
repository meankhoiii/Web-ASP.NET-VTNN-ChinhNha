using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Application.Interfaces;

/// <summary>Service for wishlist/favorite management</summary>
public interface IWishlistService
{
    /// <summary>Get user's wishlists with items</summary>
    Task<IEnumerable<WishlistDto>> GetUserWishlistsAsync(string userId);

    /// <summary>Add product to wishlist</summary>
    Task<WishlistDto> AddToWishlistAsync(string userId, CreateWishlistDto request);

    /// <summary>Remove product from wishlist</summary>
    Task<bool> RemoveFromWishlistAsync(string userId, int productId);

    /// <summary>Check if product is in user's wishlist</summary>
    Task<bool> IsProductInWishlistAsync(string userId, int productId);

    /// <summary>Get product price changes in wishlist</summary>
    Task<IEnumerable<WishlistDto>> GetPriceChangesAsync(string userId);

    /// <summary>Mark wishlist item as purchased</summary>
    Task<bool> MarkAsPurchasedAsync(string userId, int productId);

    /// <summary>Update wishlist item details</summary>
    Task<WishlistDto?> UpdateWishlistItemAsync(string userId, int wishlistId, UpdateWishlistDto request);

    /// <summary>Get wishlist item count for user</summary>
    Task<int> GetWishlistCountAsync(string userId);
}
