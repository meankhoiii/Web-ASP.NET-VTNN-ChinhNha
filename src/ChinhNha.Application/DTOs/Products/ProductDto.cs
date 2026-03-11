namespace ChinhNha.Application.DTOs.Products;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? UsageInstructions { get; set; }
    public string? TechnicalInfo { get; set; }

    public int CategoryId { get; set; }
    public string? CategoryName { get; set; } // Flattened from ProductCategory

    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; } // Flattened from Supplier

    public decimal BasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    
    public string Unit { get; set; } = string.Empty;
    public decimal? Weight { get; set; }
    
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
    public bool HasVariants { get; set; }
    public int ViewCount { get; set; }

    public string? ManufacturerUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
}
