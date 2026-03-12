using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Application.Interfaces;

public interface IProductSearchService
{
    /// <summary>Advanced product search with filters and sorting</summary>
    Task<ProductSearchPagedResultDto> SearchProductsAsync(ProductSearchFilterDto filters);
    
    /// <summary>Get search facets for filter UI</summary>
    Task<ProductSearchFacetsDto> GetSearchFacetsAsync(ProductSearchFilterDto? baseFilters = null);
    
    /// <summary>Get autocomplete suggestions for search</summary>
    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int limit = 10);
    
    /// <summary>Save user search filter for quick reuse</summary>
    Task<SavedSearchFilterDto> SaveSearchFilterAsync(string userId, string filterName, ProductSearchFilterDto filters);
    
    /// <summary>Get user's saved search filters</summary>
    Task<IEnumerable<SavedSearchFilterDto>> GetUserSavedFiltersAsync(string userId);
    
    /// <summary>Load and apply saved search filter</summary>
    Task<ProductSearchPagedResultDto> ApplySavedFilterAsync(string userId, int savedFilterId);
    
    /// <summary>Delete saved search filter</summary>
    Task<bool> DeleteSavedFilterAsync(string userId, int savedFilterId);
    
    /// <summary>Get trending search queries</summary>
    Task<IEnumerable<string>> GetTrendingSearchesAsync(int limit = 10);
}
