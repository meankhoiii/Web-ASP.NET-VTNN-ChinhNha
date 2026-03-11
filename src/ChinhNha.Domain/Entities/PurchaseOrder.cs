using ChinhNha.Domain.Enums;
using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Domain.Entities;

public class PurchaseOrder : BaseEntity, IAggregateRoot
{
    public string POCode { get; set; } = string.Empty;
    
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    
    public decimal TotalAmount { get; set; }
    public bool IsAISuggested { get; set; } = false; // Distinguish manual vs AI-suggested PO
    
    public string? Note { get; set; }
    
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    
    public string? CreatedById { get; set; }
    public AppUser? CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

