using ChinhNha.Domain.Enums;

namespace ChinhNha.Application.DTOs.Inventory;

public class InventoryTransactionDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    
    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    
    public decimal? UnitCost { get; set; }
    
    public int? OrderId { get; set; }
    public int? PurchaseOrderId { get; set; }
    
    public string? Note { get; set; }
    public DateTime TransactionDate { get; set; }
    
    public string? CreatedByUserId { get; set; }
}
