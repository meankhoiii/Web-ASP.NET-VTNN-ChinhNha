using ChinhNha.Application.DTOs.Orders;

namespace ChinhNha.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync(ChinhNha.Domain.Enums.OrderStatus? status = null);
    
    // Core Checkout logic
    Task<OrderDto> CreateOrderFromCartAsync(
        int cartId,
        string? userId,
        string shippingName,
        string shippingPhone,
        string shippingAddress,
        string shippingProvince,
        string shippingDistrict,
        string shippingWard,
        string? shippingNote,
        string paymentMethod,
        string? receiverEmail = null);
    
    Task<bool> UpdateOrderStatusAsync(int orderId, ChinhNha.Domain.Enums.OrderStatus newStatus);
    Task<bool> UpdatePaymentResultAsync(int orderId, ChinhNha.Domain.Enums.PaymentMethod paymentMethod, ChinhNha.Domain.Enums.PaymentStatus paymentStatus, string? transactionId = null, string? note = null);
    Task<bool> CancelOrderAsync(int orderId, string reason);
}
