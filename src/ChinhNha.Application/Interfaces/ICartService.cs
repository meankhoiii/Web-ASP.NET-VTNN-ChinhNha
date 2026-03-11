using ChinhNha.Application.DTOs.Orders;

namespace ChinhNha.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string? userId, string sessionId);
    Task<CartDto> AddItemToCartAsync(string? userId, string sessionId, int productId, int quantity, int? variantId = null);
    Task<CartDto> UpdateCartItemQuantityAsync(int cartItemId, int quantity);
    Task<bool> RemoveItemFromCartAsync(int cartItemId);
    Task<bool> ClearCartAsync(int cartId);
    Task<CartDto> MergeGuestCartToUserCartAsync(string sessionId, string userId);
}
