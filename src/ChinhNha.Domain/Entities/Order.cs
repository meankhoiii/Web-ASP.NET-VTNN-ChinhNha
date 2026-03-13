using ChinhNha.Domain.Enums;
using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Domain.Entities;

public class Order : BaseEntity, IAggregateRoot
{
    public string OrderCode { get; set; } = string.Empty;

    public string? UserId { get; set; }
    public AppUser? User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }

    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string? ReceiverEmail { get; set; }
    public string ShippingProvince { get; set; } = string.Empty;
    public string ShippingDistrict { get; set; } = string.Empty;
    public string ShippingWard { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;

    public string? Note { get; set; }
    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
