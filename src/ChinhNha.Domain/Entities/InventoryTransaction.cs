using ChinhNha.Domain.Enums;

namespace ChinhNha.Domain.Entities;

public class InventoryTransaction : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public TransactionType TransactionType { get; set; }
    
    public int Quantity { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    
    public string? ReferenceType { get; set; } // Order, PurchaseOrder, Manual
    public int? ReferenceId { get; set; }
    
    public decimal? UnitCost { get; set; }
    public string? Note { get; set; }
    
    public string? CreatedById { get; set; }
    public AppUser? CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
