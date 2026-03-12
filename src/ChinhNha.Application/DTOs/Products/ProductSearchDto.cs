namespace ChinhNha.Application.DTOs.Products;

/// <summary>
/// Advanced product search filter criteria
/// </summary>
public class ProductSearchFilterDto
{
    /// <summary>Search query for product name, SKU, description</summary>
    public string? SearchQuery { get; set; }

    /// <summary>Filter by category ID</summary>
    public int? CategoryId { get; set; }

    /// <summary>Filter by supplier ID</summary>
    public int? SupplierId { get; set; }

    /// <summary>Minimum price filter</summary>
    public decimal? MinPrice { get; set; }

    /// <summary>Maximum price filter</summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>Minimum rating filter (1-5)</summary>
    public int? MinRating { get; set; }

    /// <summary>Filter for new arrivals (days)</summary>
    public int? NewArrivalsWithinDays { get; set; }

    /// <summary>Filter for sale items (has sale price)</summary>
    public bool? OnSaleOnly { get; set; }

    /// <summary>Filter for in-stock items only</summary>
    public bool? InStockOnly { get; set; }

    /// <summary>Filter for featured items</summary>
    public bool? FeaturedOnly { get; set; }

    /// <summary>Sorting option: name, price, rating, newest, bestseller</summary>
    public string SortBy { get; set; } = "newest"; // Default: newest

    /// <summary>Sort order: asc or desc</summary>
    public string SortOrder { get; set; } = "desc"; // Default: descending

    /// <summary>Page number for pagination</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Items per page</summary>
    public int PageSize { get; set; } = 12;
}

/// <summary>
/// Product search result with metadata
/// </summary>
public class ProductSearchResultDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? ShortDescription { get; set; }
    
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    
    public decimal BasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public int StockQuantity { get; set; }
    
    public int ReviewCount { get; set; }
    public decimal AverageRating { get; set; }
    
    public bool IsFeatured { get; set; }
    public bool IsOnSale { get; set; }
    public decimal? DiscountPercent { get; set; }
    
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Paginated search results
/// </summary>
public class ProductSearchPagedResultDto
{
    public IEnumerable<ProductSearchResultDto> Products { get; set; } = new List<ProductSearchResultDto>();
    
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    
    public ProductSearchFilterDto AppliedFilters { get; set; } = new();
}

/// <summary>
/// Search filter preferences for customers
/// </summary>
public class SavedSearchFilterDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FilterName { get; set; } = string.Empty;
    public ProductSearchFilterDto Filters { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int UsageCount { get; set; }
}

/// <summary>
/// Search facets/aggregations for filter UI
/// </summary>
public class ProductSearchFacetsDto
{
    /// <summary>Category facets with counts</summary>
    public Dictionary<int, (string Name, int Count)> Categories { get; set; } = new();
    
    /// <summary>Supplier facets with counts</summary>
    public Dictionary<int, (string Name, int Count)> Suppliers { get; set; } = new();
    
    /// <summary>Price range statistics</summary>
    public PriceRangeDto PriceRange { get; set; } = new();
    
    /// <summary>Rating distribution</summary>
    public Dictionary<int, int> RatingDistribution { get; set; } = new(); // Rating -> Count
    
    /// <summary>Total products matching base criteria</summary>
    public int TotalProducts { get; set; }
}

/// <summary>
/// Price range statistics
/// </summary>
public class PriceRangeDto
{
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AveragePrice { get; set; }
}
