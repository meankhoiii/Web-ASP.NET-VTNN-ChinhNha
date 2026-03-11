namespace ChinhNha.Application.DTOs.Inventory;

public class InventoryForecastDto
{
    public int Id { get; set; }
    
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    
    public DateTime ForecastDate { get; set; } // Ngày sinh ra dự đoán
    public DateTime TargetDate { get; set; }   // Ngày được dự đoán (thường là tuần/tháng tới)
    
    public decimal PredictedDemand { get; set; }
    public decimal? ConfidenceLower { get; set; }
    public decimal? ConfidenceUpper { get; set; }
    
    public bool IsHistorical { get; set; }
    
    public decimal? ActualDemand { get; set; } 
    public decimal? MAPE { get; set; }
}
