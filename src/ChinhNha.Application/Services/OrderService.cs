using AutoMapper;
using ChinhNha.Application.DTOs.Orders;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Enums;
using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IMapper _mapper;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IInventoryService inventoryService,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _inventoryService = inventoryService;
        _mapper = mapper;
    }

    public async Task<bool> CancelOrderAsync(int orderId, string reason)
    {
        var order = await _orderRepository.GetOrderWithDetailsByIdAsync(orderId);
        if (order == null || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
            return false;

        order.Status = OrderStatus.Cancelled;
        order.CancelReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        // Hoàn lại kho
        foreach (var item in order.OrderItems)
        {
            await _inventoryService.RecordTransactionAsync(
                productId: item.ProductId,
                type: TransactionType.Return,
                quantity: item.Quantity,
                note: $"Hoàn trả kho từ đơn hàng #{order.Id} bị hủy",
                variantId: item.ProductVariantId,
                orderId: order.Id
            );
        }

        await _orderRepository.UpdateAsync(order);
        return true;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(int cartId, string userId, string shippingName, string shippingPhone, string shippingAddress, string? shippingNote)
    {
        var cart = await _cartRepository.GetCartWithItemsAsync(userId, string.Empty);
        if (cart == null || !cart.CartItems.Any())
            throw new InvalidOperationException("Giỏ hàng trống.");

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ReceiverName = shippingName,
            ReceiverPhone = shippingPhone,
            ShippingAddress = shippingAddress,
            Note = shippingNote,
            ShippingFee = 30000,
            Discount = 0,
            OrderItems = cart.CartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                ProductVariantId = ci.ProductVariantId,
                ProductName = ci.Product?.Name ?? string.Empty,
                VariantName = ci.ProductVariant?.VariantName,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                TotalPrice = ci.Quantity * ci.UnitPrice
            }).ToList()
        };

        order.SubTotal = order.OrderItems.Sum(i => i.TotalPrice);
        order.TotalAmount = order.SubTotal + order.ShippingFee - order.Discount;

        await _orderRepository.AddAsync(order);

        // Deduct inventory for each item
        foreach (var item in order.OrderItems)
        {
            await _inventoryService.RecordTransactionAsync(
                productId: item.ProductId,
                type: TransactionType.Export,
                quantity: item.Quantity,
                note: $"Xuất kho bán hàng. Đơn #{order.Id}",
                variantId: item.ProductVariantId,
                orderId: order.Id
            );
        }

        // Clear cart after order is created
        await _cartRepository.DeleteAsync(cart);

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _orderRepository.GetOrderWithDetailsByIdAsync(id);
        return order == null ? null : _mapper.Map<OrderDto>(order);
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _orderRepository.GetUserOrdersWithDetailsAsync(userId);
        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) return false;

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepository.UpdateAsync(order);
        return true;
    }
}
