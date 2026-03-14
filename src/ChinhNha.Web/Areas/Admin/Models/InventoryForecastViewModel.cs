using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Web.Areas.Admin.Models;

public class InventoryForecastViewModel
{
    public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
    public int? SelectedProductId { get; set; }
    public ProductDto? SelectedProduct { get; set; }
    public DateTime? LastModelUpdatedAt { get; set; }
    public IEnumerable<WeeklyDemandPointViewModel> ActualWeeklyDemands { get; set; } = new List<WeeklyDemandPointViewModel>();
    public IEnumerable<InventoryAlertItemViewModel> AlertItems { get; set; } = new List<InventoryAlertItemViewModel>();
    public string AlertFilter { get; set; } = "need";
    public ReorderAnalysisDto? ReorderAnalysis { get; set; }
    public decimal? LatestMape { get; set; }
    public decimal AvgForecastDemand { get; set; }
    public decimal? ConfidenceLower { get; set; }
    public decimal? ConfidenceUpper { get; set; }
    public decimal? ConfidenceWidthPercent { get; set; }
    
    public IEnumerable<InventoryForecastDto>? Forecasts { get; set; }
}

public class WeeklyDemandPointViewModel
{
    public DateTime WeekStart { get; set; }
    public decimal Quantity { get; set; }
}

public class InventoryAlertItemViewModel
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
    public decimal ReorderPoint { get; set; }
    public string StatusCode { get; set; } = "ok";
    public string StatusLabel { get; set; } = "Đủ hàng";
    public decimal UrgencyScore { get; set; }
}
