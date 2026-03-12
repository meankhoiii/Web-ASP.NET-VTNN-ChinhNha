using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace ChinhNha.Application.Services;

/// <summary>Wishlist service implementation</summary>
public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(
        IWishlistRepository wishlistRepository,
        IProductRepository productRepository,
        IMapper mapper,
        ILogger<WishlistService> logger)
    {
        _wishlistRepository = wishlistRepository ?? throw new ArgumentNullException(nameof(wishlistRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Get user's wishlists with items</summary>
    public async Task<IEnumerable<WishlistDto>> GetUserWishlistsAsync(string userId)
    {
        try
        {
            var wishlistItems = await _wishlistRepository.GetUserWishlistsAsync(userId);
            var result = new List<WishlistDto>();

            foreach (var item in wishlistItems)
            {
                var product = item.Product;
                if (product == null)
                    product = await _productRepository.GetByIdAsync(item.ProductId);

                if (product != null)
                {
                    var dto = new WishlistDto
                    {
                        Id = item.Id,
                        UserId = item.UserId,
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        ProductImage = product.Images?.FirstOrDefault()?.ImageUrl,
                        ProductPrice = product.BasePrice,
                        ProductSalePrice = product.SalePrice,
                        WishlistName = item.WishlistName,
                        IsDefault = item.IsDefault,
                        Notes = item.Notes,
                        Priority = item.Priority,
                        AddedAt = item.AddedAt,
                        PurchasedAt = item.PurchasedAt,
                        PriceWhenAdded = item.PriceWhenAdded,
                        PriceChange = item.PriceWhenAdded.HasValue 
                            ? product.SalePrice ?? product.BasePrice - item.PriceWhenAdded.Value
                            : null
                    };
                    result.Add(dto);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting wishlists for user {userId}");
            throw;
        }
    }

    /// <summary>Add product to wishlist</summary>
    public async Task<WishlistDto> AddToWishlistAsync(string userId, CreateWishlistDto request)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId);
            if (product == null)
                throw new Exception("Product not found");

            var wishlist = await _wishlistRepository.AddToWishlistAsync(
                userId,
                request.ProductId,
                product.SalePrice ?? product.BasePrice,
                request.Notes);

            _logger.LogInformation($"Added product {request.ProductId} to wishlist for user {userId}");

            return new WishlistDto
            {
                Id = wishlist.Id,
                UserId = wishlist.UserId,
                ProductId = wishlist.ProductId,
                ProductName = product.Name,
                ProductImage = product.Images?.FirstOrDefault()?.ImageUrl,
                ProductPrice = product.BasePrice,
                ProductSalePrice = product.SalePrice,
                WishlistName = request.WishlistName,
                IsDefault = true,
                Notes = request.Notes,
                Priority = request.Priority,
                AddedAt = wishlist.AddedAt,
                PriceWhenAdded = wishlist.PriceWhenAdded
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding product to wishlist for user {userId}");
            throw;
        }
    }

    /// <summary>Remove product from wishlist</summary>
    public async Task<bool> RemoveFromWishlistAsync(string userId, int productId)
    {
        try
        {
            var result = await _wishlistRepository.RemoveFromWishlistAsync(userId, productId);
            if (result)
                _logger.LogInformation($"Removed product {productId} from wishlist for user {userId}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing product from wishlist for user {userId}");
            throw;
        }
    }

    /// <summary>Check if product is in user's wishlist</summary>
    public async Task<bool> IsProductInWishlistAsync(string userId, int productId)
    {
        try
        {
            return await _wishlistRepository.IsProductInWishlistAsync(userId, productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking wishlist for user {userId}");
            throw;
        }
    }

    /// <summary>Get product price changes in wishlist</summary>
    public async Task<IEnumerable<WishlistDto>> GetPriceChangesAsync(string userId)
    {
        try
        {
            var priceDrops = await _wishlistRepository.GetPriceDropsAsync(userId);
            var result = new List<WishlistDto>();

            foreach (var item in priceDrops)
            {
                var product = item.Product ?? await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    result.Add(new WishlistDto
                    {
                        Id = item.Id,
                        UserId = item.UserId,
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        ProductImage = product.Images?.FirstOrDefault()?.ImageUrl,
                        ProductPrice = product.BasePrice,
                        ProductSalePrice = product.SalePrice,
                        PriceWhenAdded = item.PriceWhenAdded,
                        PriceChange = item.PriceWhenAdded.HasValue
                            ? (product.SalePrice ?? product.BasePrice) - item.PriceWhenAdded.Value
                            : null
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting price changes for user {userId}");
            throw;
        }
    }

    /// <summary>Mark wishlist item as purchased</summary>
    public async Task<bool> MarkAsPurchasedAsync(string userId, int productId)
    {
        try
        {
            var wishlistItems = await _wishlistRepository.GetUserWishlistsAsync(userId);
            var item = wishlistItems.FirstOrDefault(w => w.ProductId == productId);

            if (item == null)
                return false;

            item.PurchasedAt = DateTime.UtcNow;
            await _wishlistRepository.UpdateAsync(item);

            _logger.LogInformation($"Marked product {productId} as purchased in wishlist for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking as purchased for user {userId}");
            throw;
        }
    }

    /// <summary>Update wishlist item details</summary>
    public async Task<WishlistDto?> UpdateWishlistItemAsync(string userId, int wishlistId, UpdateWishlistDto request)
    {
        try
        {
            var item = await _wishlistRepository.GetWishlistByIdAsync(wishlistId, userId);
            if (item == null)
                return null;

            if (!string.IsNullOrWhiteSpace(request.WishlistName))
                item.WishlistName = request.WishlistName;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                item.Notes = request.Notes;
            if (request.Priority.HasValue)
                item.Priority = request.Priority.Value;
            if (request.IsDefault.HasValue)
                item.IsDefault = request.IsDefault.Value;

            await _wishlistRepository.UpdateAsync(item);

            var product = await _productRepository.GetByIdAsync(item.ProductId);
            return new WishlistDto
            {
                Id = item.Id,
                UserId = item.UserId,
                ProductId = item.ProductId,
                ProductName = product?.Name,
                ProductImage = product?.Images?.FirstOrDefault()?.ImageUrl,
                ProductPrice = product?.BasePrice ?? 0,
                ProductSalePrice = product?.SalePrice,
                WishlistName = item.WishlistName,
                IsDefault = item.IsDefault,
                Notes = item.Notes,
                Priority = item.Priority,
                AddedAt = item.AddedAt,
                PriceWhenAdded = item.PriceWhenAdded
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating wishlist item for user {userId}");
            throw;
        }
    }

    /// <summary>Get wishlist item count for user</summary>
    public async Task<int> GetWishlistCountAsync(string userId)
    {
        try
        {
            return await _wishlistRepository.GetWishlistCountAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting wishlist count for user {userId}");
            throw;
        }
    }
}
