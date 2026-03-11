namespace ChinhNha.Application.DTOs.Orders;

public class CartItemDto
{
    public int Id { get; set; }
    public int CartId { get; set; }
    
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    public decimal TotalPrice => Quantity * UnitPrice;
}
