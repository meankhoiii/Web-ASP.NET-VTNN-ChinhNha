namespace ChinhNha.Application.DTOs.Inventory;

public class ReorderAnalysisDto
{
    public int ProductId { get; set; }
    public decimal AvgDailySales { get; set; }
    public decimal MaxDailySales { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public int CurrentStock { get; set; }
    public bool NeedsReorder { get; set; }
    public decimal SuggestedOrderQty { get; set; }
    public int LeadTimeDays { get; set; }
}
