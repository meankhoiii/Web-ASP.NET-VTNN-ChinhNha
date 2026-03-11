using ChinhNha.Domain.Enums;

namespace ChinhNha.Application.DTOs.Inventory;

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    
    public PurchaseOrderStatus Status { get; set; }
    
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}
