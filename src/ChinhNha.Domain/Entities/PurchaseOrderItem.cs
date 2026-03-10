namespace ChinhNha.Domain.Entities;

public class PurchaseOrderItem : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; } = 0;
    
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}
