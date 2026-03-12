using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

/// <summary>
/// Repository interface for search analytics data
/// </summary>
public interface ISearchAnalyticsRepository : IRepository<SearchAnalytics>
{
    /// <summary>Log a search query</summary>
    Task LogSearchAsync(string query, int resultCount, string? userId = null, string? filtersJson = null);

    /// <summary>Get trending searches for a time period</summary>
    Task<IEnumerable<SearchAnalytics>> GetTrendingSearchesAsync(int days = 30, int limit = 10);

    /// <summary>Get search history for a user</summary>
    Task<IEnumerable<SearchAnalytics>> GetUserSearchHistoryAsync(string userId, int limit = 20);

    /// <summary>Get search by exact query</summary>
    Task<SearchAnalytics?> GetByQueryAsync(string query);

    /// <summary>Get searches by partial query match</summary>
    Task<IEnumerable<SearchAnalytics>> SearchByPartialQueryAsync(string query, int limit = 10);

    /// <summary>Mark search as converted to view</summary>
    Task MarkAsConvertedAsync(int searchId, int productId);

    /// <summary>Get search statistics for dashboard</summary>
    Task<(int TotalSearches, int UniqueSearches, int UniqueUsers, double ConversionRate, int? AvgResults)> GetSearchStatsAsync(int days = 30);

    /// <summary>Get top categories searched</summary>
    Task<Dictionary<string, int>> GetTopCategoriesSearchedAsync(int days = 30, int limit = 10);
}
