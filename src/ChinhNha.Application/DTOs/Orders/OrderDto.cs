using ChinhNha.Domain.Enums;

namespace ChinhNha.Application.DTOs.Orders;

public class OrderDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    
    // Payment info: đơn giản, lưu dạng string vì Order entity chưa có payment fields
    public string PaymentMethod { get; set; } = "COD";
    public bool IsPaid { get; set; } = false;
    
    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Shipping address - mapped from ReceiverName/ReceiverPhone on entity
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? ShippingNote { get; set; }

    public string? TrackingNumber { get; set; }
    public string? CancelReason { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}

