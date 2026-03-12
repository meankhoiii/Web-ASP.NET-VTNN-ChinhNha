namespace ChinhNha.Web.Areas.Admin.Models;

public class DashboardViewModel
{
    public int NewOrdersCount { get; set; }
    public decimal DeliveredRevenue { get; set; }
    public int LowStockCount { get; set; }
    public int TotalProducts { get; set; }

    public List<LowStockProductViewModel> LowStockProducts { get; set; } = new();
    public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
    public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = new();
    public List<ForecastMiniChartViewModel> ForecastMiniCharts { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

public class LowStockProductViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
}

public class RecentOrderViewModel
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class TopSellingProductViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int SoldQuantity { get; set; }
    public decimal Revenue { get; set; }
}

public class ForecastMiniChartViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ChartTitle { get; set; } = string.Empty;
    public string DatasetLabel { get; set; } = string.Empty;
    public string YAxisLabel { get; set; } = string.Empty;
    public string UnitLabel { get; set; } = string.Empty;
    public string TrendLabel { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public List<decimal> Values { get; set; } = new();
}