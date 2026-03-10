using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Domain.Entities;

public class Product : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? UsageInstructions { get; set; }
    public string? TechnicalInfo { get; set; }

    public int CategoryId { get; set; }
    public ProductCategory Category { get; set; } = null!;

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public decimal BasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public int StockQuantity { get; set; } = 0;
    public int MinStockLevel { get; set; } = 5; // Cấu hình tối thiểu để AI Alert
    
    public string Unit { get; set; } = "Sản phẩm";
    public decimal? Weight { get; set; }
    
    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public bool HasVariants { get; set; } = false;
    public int ViewCount { get; set; } = 0;

    public string? ManufacturerUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
