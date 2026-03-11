namespace ChinhNha.Application.DTOs.Requests;

public class UpdateProductRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? UsageInstructions { get; set; }
    public string? TechnicalInfo { get; set; }
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? Weight { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
    public string? ManufacturerUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? NewImageUrl { get; set; }
}
