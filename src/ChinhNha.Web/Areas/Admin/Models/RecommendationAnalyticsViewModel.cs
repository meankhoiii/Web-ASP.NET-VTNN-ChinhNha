using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Web.Areas.Admin.Models;

public class RecommendationAnalyticsViewModel
{
    public int Days { get; set; } = 30;
    public double ClickThroughRate { get; set; }
    public double ConversionRate { get; set; }
    public int AverageScore { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public string? PreviewUserId { get; set; }
    public int PreviewLimit { get; set; } = 8;

    public List<ProductRecommendationDto> PersonalizedRecommendations { get; set; } = new();
    public List<ProductRecommendationDto> TrendingRecommendations { get; set; } = new();

    public Dictionary<string, int> ReasonDistribution { get; set; } = new();
}
