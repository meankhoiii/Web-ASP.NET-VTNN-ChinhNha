namespace ChinhNha.Domain.Entities;

/// <summary>
/// Tracks search queries and analytics for trending searches
/// </summary>
public class SearchAnalytics : BaseEntity
{
    /// <summary>The search query string</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Number of times this query was searched</summary>
    public int SearchCount { get; set; } = 1;

    /// <summary>Number of results returned for this query</summary>
    public int ResultCount { get; set; }

    /// <summary>User ID who performed the search (null for anonymous)</summary>
    public string? UserId { get; set; }
    public AppUser? User { get; set; }

    /// <summary>Applied filters as JSON</summary>
    public string? FiltersJson { get; set; }

    /// <summary>Timestamp of first search</summary>
    public DateTime FirstSearchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp of most recent search</summary>
    public DateTime LastSearchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Average time spent viewing search results (in seconds)</summary>
    public int? AverageViewTimeSeconds { get; set; }

    /// <summary>Whether this search led to a product view/purchase</summary>
    public bool ConvertedToView { get; set; }
    
    public int? ConvertedProductId { get; set; }
}
