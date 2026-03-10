using ChinhNha.Domain.Enums;

namespace ChinhNha.Domain.Entities;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; } // Mã GD VNPay
    
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
