using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IProductSearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(IProductSearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Advanced product search with filters, sorting, and pagination
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<ProductSearchPagedResultDto>> SearchProducts([FromBody] ProductSearchFilterDto filters)
    {
        try
        {
            var result = await _searchService.SearchProductsAsync(filters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in product search");
            return BadRequest(new { message = "Lỗi khi tìm kiếm sản phẩm", error = ex.Message });
        }
    }

    /// <summary>
    /// Get search facets/filters for UI
    /// </summary>
    [HttpPost("facets")]
    public async Task<ActionResult<ProductSearchFacetsDto>> GetSearchFacets([FromBody] ProductSearchFilterDto? baseFilters = null)
    {
        try
        {
            var facets = await _searchService.GetSearchFacetsAsync(baseFilters);
            return Ok(facets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search facets");
            return BadRequest(new { message = "Lỗi khi lấy bộ lọc tìm kiếm" });
        }
    }

    /// <summary>
    /// Get search suggestions/autocomplete
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<IEnumerable<string>>> GetSearchSuggestions([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new List<string>());

            var suggestions = await _searchService.GetSearchSuggestionsAsync(query, limit);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions");
            return BadRequest(new { message = "Lỗi khi lấy gợi ý tìm kiếm" });
        }
    }

    /// <summary>
    /// Get trending searches
    /// </summary>
    [HttpGet("trending")]
    public async Task<ActionResult<IEnumerable<string>>> GetTrendingSearches([FromQuery] int limit = 10)
    {
        try
        {
            var trends = await _searchService.GetTrendingSearchesAsync(limit);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending searches");
            return BadRequest(new { message = "Lỗi khi lấy tìm kiếm phổ biến" });
        }
    }

    /// <summary>
    /// Save search filter for logged-in user
    /// </summary>
    [HttpPost("saved-filters")]
    public async Task<ActionResult<SavedSearchFilterDto>> SaveSearchFilter(
        [FromBody] SaveSearchFilterRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập để lưu bộ lọc" });

            var result = await _searchService.SaveSearchFilterAsync(userId, request.FilterName, request.Filters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving search filter");
            return BadRequest(new { message = "Lỗi khi lưu bộ lọc tìm kiếm" });
        }
    }

    /// <summary>
    /// Get user's saved search filters
    /// </summary>
    [HttpGet("saved-filters")]
    public async Task<ActionResult<IEnumerable<SavedSearchFilterDto>>> GetUserSavedFilters()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var result = await _searchService.GetUserSavedFiltersAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved filters");
            return BadRequest(new { message = "Lỗi khi lấy bộ lọc đã lưu" });
        }
    }

    /// <summary>
    /// Apply saved filter and search
    /// </summary>
    [HttpGet("saved-filters/{filterId}")]
    public async Task<ActionResult<ProductSearchPagedResultDto>> ApplySavedFilter(int filterId)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var result = await _searchService.ApplySavedFilterAsync(userId, filterId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying saved filter");
            return BadRequest(new { message = "Lỗi khi áp dụng bộ lọc đã lưu" });
        }
    }

    /// <summary>
    /// Delete saved search filter
    /// </summary>
    [HttpDelete("saved-filters/{filterId}")]
    public async Task<ActionResult> DeleteSavedFilter(int filterId)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var result = await _searchService.DeleteSavedFilterAsync(userId, filterId);
            if (!result)
                return NotFound(new { message = "Không tìm thấy bộ lọc tìm kiếm" });

            return Ok(new { message = "Bộ lọc tìm kiếm đã được xóa" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved filter");
            return BadRequest(new { message = "Lỗi khi xóa bộ lọc tìm kiếm" });
        }
    }

    /// <summary>
    /// Request model for saving search filters
    /// </summary>
    public class SaveSearchFilterRequest
    {
        public string FilterName { get; set; } = string.Empty;
        public ProductSearchFilterDto Filters { get; set; } = new();
    }
}
