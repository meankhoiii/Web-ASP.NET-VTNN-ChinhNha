namespace ChinhNha.Application.DTOs.Products;

/// <summary>
/// Search analytics data transfer object
/// </summary>
public class SearchAnalyticsDto
{
    public int Id { get; set; }
    public string Query { get; set; } = string.Empty;
    public int SearchCount { get; set; }
    public int ResultCount { get; set; }
    public string? UserId { get; set; }
    public DateTime FirstSearchedAt { get; set; }
    public DateTime LastSearchedAt { get; set; }
    public int? AverageViewTimeSeconds { get; set; }
    public bool ConvertedToView { get; set; }
    public int? ConvertedProductId { get; set; }
}

/// <summary>
/// Create search analytics request
/// </summary>
public class CreateSearchAnalyticsDto
{
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public string? FiltersJson { get; set; }
}

/// <summary>
/// Trending search result
/// </summary>
public class TrendingSearchDto
{
    public string Query { get; set; } = string.Empty;
    public int SearchCount { get; set; }
    public int ResultCount { get; set; }
    public DateTime LastSearchedAt { get; set; }
    public int Rank { get; set; }
}

/// <summary>
/// Search statistics summary
/// </summary>
public class SearchStatisticsDto
{
    public int TotalSearches { get; set; }
    public int UniqueSearches { get; set; }
    public int UniqueUsers { get; set; } 
    public double ConversionRate { get; set; }
    public int? AverageResultsPerSearch { get; set; }
    public IEnumerable<TrendingSearchDto> TrendingSearches { get; set; } = new List<TrendingSearchDto>();
    public Dictionary<string, int> SearchesByCategory { get; set; } = new();
}
