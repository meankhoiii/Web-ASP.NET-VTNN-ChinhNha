using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;

namespace ChinhNha.Infrastructure.Repositories;

/// <summary>Wishlist repository implementation</summary>
public class WishlistRepository : GenericRepository<Wishlist>, IWishlistRepository
{
    public WishlistRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>Get all wishlists for a user</summary>
    public async Task<IEnumerable<Wishlist>> GetUserWishlistsAsync(string userId)
    {
        var all = await ListAllAsync();
        return all
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.IsDefault)
            .ThenByDescending(w => w.AddedAt)
            .ToList();
    }

    /// <summary>Get user's default wishlist</summary>
    public async Task<Wishlist?> GetDefaultWishlistAsync(string userId)
    {
        var all = await ListAllAsync();
        return all.FirstOrDefault(w => w.UserId == userId && w.IsDefault);
    }

    /// <summary>Check if product is in user's wishlist</summary>
    public async Task<bool> IsProductInWishlistAsync(string userId, int productId)
    {
        var all = await ListAllAsync();
        return all.Any(w => w.UserId == userId && w.ProductId == productId && w.PurchasedAt == null);
    }

    /// <summary>Get wishlist by ID and verify ownership</summary>
    public async Task<Wishlist?> GetWishlistByIdAsync(int id, string userId)
    {
        var item = await GetByIdAsync(id);
        if (item != null && item.UserId == userId)
            return item;
        return null;
    }

    /// <summary>Add product to wishlist</summary>
    public async Task<Wishlist> AddToWishlistAsync(string userId, int productId, decimal currentPrice, string? notes = null)
    {
        var wishlist = new Wishlist
        {
            UserId = userId,
            ProductId = productId,
            PriceWhenAdded = currentPrice,
            Notes = notes,
            AddedAt = DateTime.UtcNow,
            IsDefault = true
        };
        
        await AddAsync(wishlist);
        return wishlist;
    }

    /// <summary>Remove product from wishlist</summary>
    public async Task<bool> RemoveFromWishlistAsync(string userId, int productId)
    {
        var all = await ListAllAsync();
        var item = all.FirstOrDefault(w => w.UserId == userId && w.ProductId == productId);
        
        if (item == null)
            return false;
            
        await DeleteAsync(item);
        return true;
    }

    /// <summary>Get products with price drops</summary>
    public async Task<IEnumerable<Wishlist>> GetPriceDropsAsync(string userId)
    {
        var all = await ListAllAsync();
        return all
            .Where(w => w.UserId == userId && 
                       w.PriceWhenAdded.HasValue && 
                       w.Product != null &&
                       w.Product.SalePrice < w.PriceWhenAdded)
            .OrderByDescending(w => (w.PriceWhenAdded.Value - (w.Product?.SalePrice ?? 0)) / w.PriceWhenAdded.Value * 100)
            .ToList();
    }

    /// <summary>Count total items in user's wishlists</summary>
    public async Task<int> GetWishlistCountAsync(string userId)
    {
        var all = await ListAllAsync();
        return all.Count(w => w.UserId == userId && w.PurchasedAt == null);
    }
}
