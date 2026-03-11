using ChinhNha.Domain.Enums;

namespace ChinhNha.Application.DTOs.Requests;

public class CreateOrderRequest
{
    public int CartId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? ShippingNote { get; set; }
}
