namespace ChinhNha.Application.DTOs.Requests;

public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int Quantity { get; set; }
}
