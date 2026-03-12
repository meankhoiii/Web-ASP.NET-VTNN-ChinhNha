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
    private readonly IPaymentRepository _paymentRepository;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IInventoryService inventoryService,
        IMapper mapper,
        IPaymentRepository paymentRepository)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _inventoryService = inventoryService;
        _mapper = mapper;
        _paymentRepository = paymentRepository;
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

    public async Task<OrderDto> CreateOrderFromCartAsync(int cartId, string userId, string shippingName, string shippingPhone, string shippingAddress, string? shippingNote, string paymentMethod)
    {
        var cart = await _cartRepository.GetCartWithItemsAsync(userId, string.Empty);
        if (cart == null || !cart.CartItems.Any())
            throw new InvalidOperationException("Giỏ hàng trống.");

        var parsedPaymentMethod = ParsePaymentMethod(paymentMethod);

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

        await _paymentRepository.AddAsync(new Payment
        {
            OrderId = order.Id,
            PaymentMethod = parsedPaymentMethod,
            PaymentStatus = parsedPaymentMethod == PaymentMethod.COD ? PaymentStatus.Pending : PaymentStatus.Pending,
            Amount = order.TotalAmount,
            Note = parsedPaymentMethod == PaymentMethod.COD
                ? "Thanh toán khi nhận hàng."
                : $"Khởi tạo thanh toán {parsedPaymentMethod}."
        });

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

        return await MapOrderDtoAsync(order);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _orderRepository.GetOrderWithDetailsByIdAsync(id);
        return order == null ? null : await MapOrderDtoAsync(order);
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _orderRepository.GetUserOrdersWithDetailsAsync(userId);
        return await MapOrderDtosAsync(orders);
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync(OrderStatus? status = null)
    {
        var orders = await _orderRepository.GetAllOrdersWithDetailsAsync();
        if (status.HasValue)
        {
            orders = orders.Where(o => o.Status == status.Value);
        }
        return await MapOrderDtosAsync(orders);
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

    public async Task<bool> UpdatePaymentResultAsync(int orderId, PaymentMethod paymentMethod, PaymentStatus paymentStatus, string? transactionId = null, string? note = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            return false;

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
        if (payment == null)
        {
            payment = new Payment
            {
                OrderId = orderId,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentStatus,
                Amount = order.TotalAmount,
                TransactionId = transactionId,
                PaidAt = paymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : null,
                Note = note
            };

            await _paymentRepository.AddAsync(payment);
        }
        else
        {
            payment.PaymentMethod = paymentMethod;
            payment.PaymentStatus = paymentStatus;
            payment.TransactionId = transactionId;
            payment.PaidAt = paymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : null;
            payment.Note = note;
            await _paymentRepository.UpdateAsync(payment);
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepository.UpdateAsync(order);
        return true;
    }

    private async Task<OrderDto> MapOrderDtoAsync(Order order)
    {
        var dto = _mapper.Map<OrderDto>(order);
        var payment = await _paymentRepository.GetByOrderIdAsync(order.Id);
        dto.PaymentMethod = payment?.PaymentMethod.ToString() ?? PaymentMethod.COD.ToString();
        dto.IsPaid = payment?.PaymentStatus == PaymentStatus.Paid;
        return dto;
    }

    private async Task<IEnumerable<OrderDto>> MapOrderDtosAsync(IEnumerable<Order> orders)
    {
        var orderList = orders.ToList();
        var orderIds = orderList.Select(o => o.Id).ToList();
        var payments = await _paymentRepository.GetByOrderIdsAsync(orderIds);

        var result = new List<OrderDto>(orderList.Count);
        foreach (var order in orderList)
        {
            var dto = _mapper.Map<OrderDto>(order);
            if (payments.TryGetValue(order.Id, out var payment))
            {
                dto.PaymentMethod = payment.PaymentMethod.ToString();
                dto.IsPaid = payment.PaymentStatus == PaymentStatus.Paid;
            }

            result.Add(dto);
        }

        return result;
    }

    private static PaymentMethod ParsePaymentMethod(string paymentMethod)
    {
        return Enum.TryParse<PaymentMethod>(paymentMethod, true, out var parsed)
            ? parsed
            : PaymentMethod.COD;
    }
}
