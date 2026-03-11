using ChinhNha.Application.DTOs.Orders;

namespace ChinhNha.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync(ChinhNha.Domain.Enums.OrderStatus? status = null);
    
    // Core Checkout logic
    Task<OrderDto> CreateOrderFromCartAsync(int cartId, string userId, string shippingName, string shippingPhone, string shippingAddress, string? shippingNote);
    
    Task<bool> UpdateOrderStatusAsync(int orderId, ChinhNha.Domain.Enums.OrderStatus newStatus);
    Task<bool> CancelOrderAsync(int orderId, string reason);
}
