namespace ChinhNha.Application.DTOs.Orders;

public class PaymentInformationDto
{
    public string OrderType { get; set; } = "other";
    public decimal Amount { get; set; }
    public string OrderDescription { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int OrderId { get; set; }
}

public class PaymentResponseDto
{
    public string OrderDescription { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string VnPayResponseCode { get; set; } = string.Empty;
}
