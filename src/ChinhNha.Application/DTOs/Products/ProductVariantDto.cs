namespace ChinhNha.Application.DTOs.Products;

public class ProductVariantDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    public string VariantName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public decimal? Weight { get; set; }
    
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}
