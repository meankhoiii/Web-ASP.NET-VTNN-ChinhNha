using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Domain.Entities;

public class PurchaseOrder : BaseEntity, IAggregateRoot
{
    public string POCode { get; set; } = string.Empty;
    
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    // 0=Draft, 1=Submitted, 2=Received, 3=Cancelled
    public int Status { get; set; } = 0; 
    
    public decimal TotalAmount { get; set; }
    public bool IsAISuggested { get; set; } = false; // Phân biệt PO tạo tay vs AI đề xuất
    
    public string? Note { get; set; }
    
    public DateTime? ExpectedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    
    public string? CreatedById { get; set; }
    public AppUser? CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
