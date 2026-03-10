namespace ChinhNha.Domain.Entities;

public class InventoryForecast : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateTime ForecastDate { get; set; }
    
    public decimal PredictedDemand { get; set; }
    public decimal? ConfidenceLower { get; set; }
    public decimal? ConfidenceUpper { get; set; }
    
    public decimal? ActualDemand { get; set; } // Cập nhật sau để đánh giá MAPE
    public decimal? MAPE { get; set; }
    
    public string? ModelVersion { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
