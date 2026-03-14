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

        var previousStatus = order.Status;

        order.Status = OrderStatus.Cancelled;
        order.CancelReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        if (IsInventoryDeductedStatus(previousStatus))
        {
            await RestoreInventoryForOrderAsync(order, "Hoàn trả kho từ đơn hàng bị hủy.");
        }

        await SyncPaymentStatusByOrderStatusAsync(order.Id, order.Status);

        await _orderRepository.UpdateAsync(order);
        return true;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(
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
        string? receiverEmail = null)
    {
        var cart = await _cartRepository.GetCartWithItemsByIdAsync(cartId);
        if (cart == null || !cart.CartItems.Any())
            throw new InvalidOperationException("Giỏ hàng trống.");

        if (!string.IsNullOrWhiteSpace(userId) && !string.Equals(cart.UserId, userId, StringComparison.Ordinal))
            throw new InvalidOperationException("Giỏ hàng không thuộc về người dùng hiện tại.");

        var parsedPaymentMethod = ParsePaymentMethod(paymentMethod);

        var order = new Order
        {
            OrderCode = GenerateOrderCode(),
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ReceiverName = shippingName,
            ReceiverPhone = shippingPhone,
            ReceiverEmail = string.IsNullOrWhiteSpace(receiverEmail) ? null : receiverEmail.Trim(),
            ShippingProvince = shippingProvince,
            ShippingDistrict = shippingDistrict,
            ShippingWard = shippingWard,
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
        var order = await _orderRepository.GetOrderWithDetailsByIdAsync(orderId);
        if (order == null) return false;

        var previousStatus = order.Status;

        if (previousStatus == newStatus)
        {
            return false;
        }

        // Final states should not be changed again; delivered can only move to returned.
        if (previousStatus == OrderStatus.Cancelled || previousStatus == OrderStatus.Returned)
        {
            return false;
        }

        if (previousStatus == OrderStatus.Delivered && newStatus != OrderStatus.Returned)
        {
            return false;
        }

        // Deduct stock once when entering operational statuses.
        if (!IsInventoryDeductedStatus(previousStatus) && IsInventoryDeductedStatus(newStatus))
        {
            await DeductInventoryForOrderAsync(order, $"Xuất kho theo trạng thái đơn {newStatus}.");
        }

        // Restore stock only if this order had already deducted stock.
        if (IsInventoryDeductedStatus(previousStatus)
            && (newStatus == OrderStatus.Cancelled || newStatus == OrderStatus.Returned))
        {
            await RestoreInventoryForOrderAsync(order, $"Hoàn kho do chuyển trạng thái đơn sang {newStatus}.");
        }

        await SyncPaymentStatusByOrderStatusAsync(order.Id, newStatus);

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
        dto.PaymentStatus = payment?.PaymentStatus.ToString() ?? PaymentStatus.Pending.ToString();
        dto.PaymentStatusDisplay = BuildPaymentStatusDisplay(payment);
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
                dto.PaymentStatus = payment.PaymentStatus.ToString();
                dto.PaymentStatusDisplay = BuildPaymentStatusDisplay(payment);
                dto.IsPaid = payment.PaymentStatus == PaymentStatus.Paid;
            }
            else
            {
                dto.PaymentMethod = PaymentMethod.COD.ToString();
                dto.PaymentStatus = PaymentStatus.Pending.ToString();
                dto.PaymentStatusDisplay = "Chờ thanh toán";
                dto.IsPaid = false;
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

    private async Task RestoreInventoryForOrderAsync(Order order, string notePrefix)
    {
        foreach (var item in order.OrderItems)
        {
            await _inventoryService.RecordTransactionAsync(
                productId: item.ProductId,
                type: TransactionType.Return,
                quantity: item.Quantity,
                note: $"{notePrefix} Đơn #{order.Id}",
                variantId: item.ProductVariantId,
                orderId: order.Id
            );
        }
    }

    private async Task DeductInventoryForOrderAsync(Order order, string notePrefix)
    {
        foreach (var item in order.OrderItems)
        {
            await _inventoryService.RecordTransactionAsync(
                productId: item.ProductId,
                type: TransactionType.Export,
                quantity: item.Quantity,
                note: $"{notePrefix} Đơn #{order.Id}",
                variantId: item.ProductVariantId,
                orderId: order.Id
            );
        }
    }

    private static bool IsInventoryDeductedStatus(OrderStatus status)
    {
        return status is OrderStatus.Confirmed
            or OrderStatus.Processing
            or OrderStatus.Shipping
            or OrderStatus.Delivered;
    }

    private async Task SyncPaymentStatusByOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
        if (payment == null)
        {
            return;
        }

        var originalStatus = payment.PaymentStatus;
        var newPaymentStatus = originalStatus;
        string? note = null;

        if (newStatus == OrderStatus.Delivered
            && payment.PaymentMethod == PaymentMethod.COD
            && originalStatus == PaymentStatus.Pending)
        {
            newPaymentStatus = PaymentStatus.Paid;
            note = "Đơn COD đã giao thành công, cập nhật đã thu tiền.";
        }
        else if (newStatus == OrderStatus.Returned && originalStatus == PaymentStatus.Paid)
        {
            newPaymentStatus = PaymentStatus.Refunded;
            note = payment.PaymentMethod == PaymentMethod.VNPay
                ? "Đơn hàng trả lại, cần xử lý hoàn tiền VNPay."
                : "Đơn COD trả lại, đã đánh dấu hoàn tiền.";
        }
        else if ((newStatus == OrderStatus.Cancelled || newStatus == OrderStatus.Returned)
                 && originalStatus == PaymentStatus.Pending)
        {
            newPaymentStatus = PaymentStatus.Failed;
            note = payment.PaymentMethod == PaymentMethod.VNPay
                ? "Đơn đã hủy/trả khi chưa thanh toán VNPay."
                : "Đơn COD đã hủy/trả trước khi thu tiền.";
        }

        if (newPaymentStatus == originalStatus)
        {
            return;
        }

        payment.PaymentStatus = newPaymentStatus;
        payment.PaidAt = newPaymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : payment.PaidAt;
        payment.Note = note ?? payment.Note;
        await _paymentRepository.UpdateAsync(payment);
    }

    private static string BuildPaymentStatusDisplay(Payment? payment)
    {
        if (payment == null)
        {
            return "Chờ thanh toán";
        }

        return payment.PaymentMethod switch
        {
            PaymentMethod.COD => payment.PaymentStatus switch
            {
                PaymentStatus.Paid => "Đã thu tiền COD",
                PaymentStatus.Pending => "Chờ thu tiền COD",
                PaymentStatus.Failed => "COD chưa thu/không thành công",
                PaymentStatus.Refunded => "COD đã hoàn tiền",
                _ => "Trạng thái COD không xác định"
            },
            PaymentMethod.VNPay => payment.PaymentStatus switch
            {
                PaymentStatus.Paid => "VNPay đã thanh toán",
                PaymentStatus.Pending => "Chờ thanh toán VNPay",
                PaymentStatus.Failed => "VNPay thanh toán thất bại",
                PaymentStatus.Refunded => "VNPay đã hoàn tiền",
                _ => "Trạng thái VNPay không xác định"
            },
            _ => payment.PaymentStatus switch
            {
                PaymentStatus.Paid => "Đã thanh toán",
                PaymentStatus.Pending => "Chờ thanh toán",
                PaymentStatus.Failed => "Thanh toán thất bại",
                PaymentStatus.Refunded => "Đã hoàn tiền",
                _ => "Trạng thái thanh toán không xác định"
            }
        };
    }

    private static string GenerateOrderCode()
    {
        // Format: CN + yyyyMMddHHmmssfff + 1 random digit => 20 chars max.
        var now = DateTime.UtcNow;
        var randomDigit = Random.Shared.Next(0, 10);
        return $"CN{now:yyyyMMddHHmmssfff}{randomDigit}";
    }
}
