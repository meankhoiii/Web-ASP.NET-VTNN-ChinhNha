using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;

namespace ChinhNha.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for search analytics
/// </summary>
public class SearchAnalyticsRepository : GenericRepository<SearchAnalytics>, ISearchAnalyticsRepository
{
    public SearchAnalyticsRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>Log a search query</summary>
    public async Task LogSearchAsync(string query, int resultCount, string? userId = null, string? filtersJson = null)
    {
        var existing = await GetByQueryAsync(query);
        
        if (existing != null)
        {
            // Update existing search record
            existing.SearchCount++;
            existing.ResultCount = resultCount;
            existing.LastSearchedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(existing.UserId))
            {
                existing.UserId = userId;
            }
        }
        else
        {
            // Create new search record
            var search = new SearchAnalytics
            {
                Query = query,
                ResultCount = resultCount,
                UserId = userId,
                FiltersJson = filtersJson,
                SearchCount = 1,
                FirstSearchedAt = DateTime.UtcNow,
                LastSearchedAt = DateTime.UtcNow
            };
            await AddAsync(search);
        }
    }

    /// <summary>Get trending searches for a time period</summary>
    public async Task<IEnumerable<SearchAnalytics>> GetTrendingSearchesAsync(int days = 30, int limit = 10)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var allSearches = await ListAllAsync();
        
        return allSearches
            .Where(s => s.LastSearchedAt >= cutoffDate)
            .OrderByDescending(s => s.SearchCount)
            .ThenByDescending(s => s.LastSearchedAt)
            .Take(limit)
            .ToList();
    }

    /// <summary>Get search history for a user</summary>
    public async Task<IEnumerable<SearchAnalytics>> GetUserSearchHistoryAsync(string userId, int limit = 20)
    {
        var allSearches = await ListAllAsync();
        
        return allSearches
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastSearchedAt)
            .Take(limit)
            .ToList();
    }

    /// <summary>Get search by exact query</summary>
    public async Task<SearchAnalytics?> GetByQueryAsync(string query)
    {
        var allSearches = await ListAllAsync();
        return allSearches.FirstOrDefault(s => s.Query.ToLower() == query.ToLower());
    }

    /// <summary>Get searches by partial query match</summary>
    public async Task<IEnumerable<SearchAnalytics>> SearchByPartialQueryAsync(string query, int limit = 10)
    {
        var allSearches = await ListAllAsync();
        var lowerQuery = query.ToLower();
        
        return allSearches
            .Where(s => s.Query.ToLower().Contains(lowerQuery))
            .OrderByDescending(s => s.SearchCount)
            .Take(limit)
            .ToList();
    }

    /// <summary>Mark search as converted to view</summary>
    public async Task MarkAsConvertedAsync(int searchId, int productId)
    {
        var search = await GetByIdAsync(searchId);
        if (search == null) return;

        search.ConvertedToView = true;
        search.ConvertedProductId = productId;
        await UpdateAsync(search);
    }

    /// <summary>Get search statistics for dashboard</summary>
    public async Task<(int TotalSearches, int UniqueSearches, int UniqueUsers, double ConversionRate, int? AvgResults)> GetSearchStatsAsync(int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var allSearches = await ListAllAsync();
        var recentSearches = allSearches.Where(s => s.LastSearchedAt >= cutoffDate).ToList();

        int totalSearches = recentSearches.Sum(s => s.SearchCount);
        int uniqueSearches = recentSearches.Count();
        int uniqueUsers = recentSearches
            .Where(s => !string.IsNullOrWhiteSpace(s.UserId))
            .Select(s => s.UserId)
            .Distinct()
            .Count();

        int convertedSearches = recentSearches.Count(s => s.ConvertedToView);
        double conversionRate = totalSearches > 0 ? (double)convertedSearches / totalSearches * 100 : 0;

        int? avgResults = recentSearches.Any() 
            ? (int)recentSearches.Average(s => s.ResultCount) 
            : null;

        return (totalSearches, uniqueSearches, uniqueUsers, conversionRate, avgResults);
    }

    /// <summary>Get top categories searched</summary>
    public async Task<Dictionary<string, int>> GetTopCategoriesSearchedAsync(int days = 30, int limit = 10)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var allSearches = await ListAllAsync();
        
        var topCategories = new Dictionary<string, int>();
        var recentSearches = allSearches
            .Where(s => s.LastSearchedAt >= cutoffDate)
            .ToList();

        // Parse category from filters if available
        foreach (var search in recentSearches)
        {
            if (!string.IsNullOrWhiteSpace(search.FiltersJson))
            {
                try
                {
                    // Try to match category keywords in query
                    var query = search.Query.ToLower();
                    if (query.Contains("điều hoà") || query.Contains("máy lạnh"))
                    {
                        IncrementCategory(topCategories, "Điều Hoà");
                    }
                    else if (query.Contains("nông cụ") || query.Contains("dụng cụ"))
                    {
                        IncrementCategory(topCategories, "Nông Cụ");
                    }
                    else if (query.Contains("phân bón") || query.Contains("phân"))
                    {
                        IncrementCategory(topCategories, "Phân Bón");
                    }
                    else if (query.Contains("hạt giống") || query.Contains("giống"))
                    {
                        IncrementCategory(topCategories, "Hạt Giống");
                    }
                }
                catch { }
            }
        }

        return topCategories
            .OrderByDescending(x => x.Value)
            .Take(limit)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    private void IncrementCategory(Dictionary<string, int> dict, string category)
    {
        if (dict.ContainsKey(category))
            dict[category]++;
        else
            dict[category] = 1;
    }
}
