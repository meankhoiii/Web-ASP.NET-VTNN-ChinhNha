using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using AutoMapper;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ChinhNha.Application.Services;

public class ProductSearchService : IProductSearchService
{
    private readonly IProductRepository _productRepository;
    private readonly ISavedSearchFilterRepository _savedFilterRepository;
    private readonly ISearchAnalyticsRepository _searchAnalyticsRepository;
    private readonly IProductReviewService _reviewService;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductSearchService> _logger;

    public ProductSearchService(
        IProductRepository productRepository,
        ISavedSearchFilterRepository savedFilterRepository,
        ISearchAnalyticsRepository searchAnalyticsRepository,
        IProductReviewService reviewService,
        IMapper mapper,
        ILogger<ProductSearchService> logger)
    {
        _productRepository = productRepository;
        _savedFilterRepository = savedFilterRepository;
        _searchAnalyticsRepository = searchAnalyticsRepository;
        _reviewService = reviewService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductSearchPagedResultDto> SearchProductsAsync(ProductSearchFilterDto filters)
    {
        // Get products matching filter criteria
        var allProducts = await _productRepository.SearchProductsAsync(
            filters.SearchQuery,
            filters.CategoryId,
            filters.SupplierId,
            filters.MinPrice,
            filters.MaxPrice,
            filters.InStockOnly,
            filters.FeaturedOnly,
            filters.OnSaleOnly);

        // Filter by rating if specified
        var filtered = allProducts.ToList();
        if (filters.MinRating.HasValue)
        {
            filtered = await FilterByMinRatingAsync(filtered, filters.MinRating.Value);
        }

        // Filter by new arrivals if specified
        if (filters.NewArrivalsWithinDays.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-filters.NewArrivalsWithinDays.Value);
            filtered = filtered.Where(p => p.CreatedAt >= cutoffDate).ToList();
        }

        // Sort products
        var sorted = ApplySorting(filtered, filters.SortBy, filters.SortOrder);

        // Calculate total before pagination
        int totalCount = sorted.Count();

        // Apply pagination
        var pagedProducts = sorted
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToList();

        // Map to DTOs and include review stats
        var results = new List<ProductSearchResultDto>();
        foreach (var product in pagedProducts)
        {
            results.Add(await MapToSearchResultDto(product));
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize);

        // Log search analytics
        try
        {
            var filtersJson = System.Text.Json.JsonSerializer.Serialize(filters);
            await _searchAnalyticsRepository.LogSearchAsync(
                filters.SearchQuery ?? "all",
                totalCount,
                null,
                filtersJson);
            _logger.LogInformation($"Search logged: '{filters.SearchQuery}' - {totalCount} results");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging search analytics");
        }

        return new ProductSearchPagedResultDto
        {
            Products = results,
            TotalCount = totalCount,
            CurrentPage = filters.PageNumber,
            PageSize = filters.PageSize,
            TotalPages = totalPages,
            AppliedFilters = filters
        };
    }

    public async Task<ProductSearchFacetsDto> GetSearchFacetsAsync(ProductSearchFilterDto? baseFilters = null)
    {
        var allProducts = baseFilters != null
            ? await _productRepository.SearchProductsAsync(
                baseFilters.SearchQuery,
                baseFilters.CategoryId,
                baseFilters.SupplierId,
                baseFilters.MinPrice,
                baseFilters.MaxPrice,
                baseFilters.InStockOnly,
                baseFilters.FeaturedOnly,
                baseFilters.OnSaleOnly)
            : await _productRepository.GetProductsWithDetailsAsync();

        var productList = allProducts.ToList();

        // Get price range
        var prices = productList.Select(p => p.SalePrice ?? p.BasePrice).ToList();
        var priceRange = new PriceRangeDto
        {
            MinPrice = prices.Any() ? prices.Min() : 0,
            MaxPrice = prices.Any() ? prices.Max() : 0,
            AveragePrice = prices.Any() ? (decimal)prices.Average(p => (double)p) : 0
        };

        // Get categories with counts
        var categoryCounts = productList
            .Where(p => p.Category != null)
            .GroupBy(p => p.Category!.Id)
            .ToDictionary(
                g => g.Key,
                g => (g.First().Category!.Name, g.Count()));

        // Get suppliers with counts
        var supplierCounts = productList
            .Where(p => p.Supplier != null && p.SupplierId.HasValue)
            .GroupBy(p => p.SupplierId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (g.First().Supplier!.Name, g.Count()));

        // Get rating distribution
        var ratingDistribution = new Dictionary<int, int>
        {
            { 5, 0 },
            { 4, 0 },
            { 3, 0 },
            { 2, 0 },
            { 1, 0 }
        };

        // TODO: Calculate rating distribution from reviews
        // For now, this would require querying review stats for each product

        return new ProductSearchFacetsDto
        {
            Categories = categoryCounts,
            Suppliers = supplierCounts,
            PriceRange = priceRange,
            RatingDistribution = ratingDistribution,
            TotalProducts = productList.Count
        };
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<string>();

        var allProducts = await _productRepository.GetProductsWithDetailsAsync();
        var lowerQuery = query.ToLower();

        var suggestions = allProducts
            .Where(p => p.IsActive && p.Name.ToLower().Contains(lowerQuery))
            .Select(p => p.Name)
            .Distinct()
            .Take(limit)
            .ToList();

        return suggestions;
    }

    public async Task<SavedSearchFilterDto> SaveSearchFilterAsync(string userId, string filterName, ProductSearchFilterDto filters)
    {
        var filtersJson = JsonSerializer.Serialize(filters);
        var savedFilter = new SavedSearchFilter
        {
            UserId = userId,
            FilterName = filterName,
            FiltersJson = filtersJson,
            CreatedAt = DateTime.UtcNow,
            UsageCount = 0
        };

        await _savedFilterRepository.AddAsync(savedFilter);

        return new SavedSearchFilterDto
        {
            Id = savedFilter.Id,
            UserId = userId,
            FilterName = filterName,
            Filters = filters,
            CreatedAt = savedFilter.CreatedAt,
            LastUsedAt = savedFilter.LastUsedAt,
            UsageCount = savedFilter.UsageCount
        };
    }

    public async Task<IEnumerable<SavedSearchFilterDto>> GetUserSavedFiltersAsync(string userId)
    {
        var filters = await _savedFilterRepository.GetUserFiltersAsync(userId);
        return filters.Select(f => new SavedSearchFilterDto
        {
            Id = f.Id,
            UserId = f.UserId,
            FilterName = f.FilterName,
            Filters = JsonSerializer.Deserialize<ProductSearchFilterDto>(f.FiltersJson) ?? new ProductSearchFilterDto(),
            CreatedAt = f.CreatedAt,
            LastUsedAt = f.LastUsedAt,
            UsageCount = f.UsageCount
        });
    }

    public async Task<ProductSearchPagedResultDto> ApplySavedFilterAsync(string userId, int savedFilterId)
    {
        var savedFilter = await _savedFilterRepository.GetUserFilterAsync(userId, savedFilterId);
        if (savedFilter == null)
            throw new KeyNotFoundException($"Saved filter {savedFilterId} not found");

        var filters = JsonSerializer.Deserialize<ProductSearchFilterDto>(savedFilter.FiltersJson) ?? new ProductSearchFilterDto();
        
        // Update last used and usage count
        savedFilter.LastUsedAt = DateTime.UtcNow;
        savedFilter.UsageCount++;
        await _savedFilterRepository.UpdateAsync(savedFilter);

        return await SearchProductsAsync(filters);
    }

    public async Task<bool> DeleteSavedFilterAsync(string userId, int savedFilterId)
    {
        return await _savedFilterRepository.DeleteUserFilterAsync(userId, savedFilterId);
    }

    public async Task<IEnumerable<string>> GetTrendingSearchesAsync(int limit = 10)
    {
        // TODO: Implement trending searches based on audit logs or search history
        // For MVP, return empty list
        return await Task.FromResult(new List<string>());
    }

    // Helper methods

    private async Task<List<Product>> FilterByMinRatingAsync(List<Product> products, int minRating)
    {
        var filtered = new List<Product>();
        foreach (var product in products)
        {
            var stats = await _reviewService.GetProductReviewStatsAsync(product.Id);
            if (stats.AverageRating >= minRating)
            {
                filtered.Add(product);
            }
        }
        return filtered;
    }

    private List<Product> ApplySorting(List<Product> products, string sortBy, string sortOrder)
    {
        var ascending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLower() switch
        {
            "name" => ascending
                ? products.OrderBy(p => p.Name).ToList()
                : products.OrderByDescending(p => p.Name).ToList(),

            "price" => ascending
                ? products.OrderBy(p => p.SalePrice ?? p.BasePrice).ToList()
                : products.OrderByDescending(p => p.SalePrice ?? p.BasePrice).ToList(),

            "rating" => ascending
                ? products.OrderBy(p => p.Id).ToList() // TODO: Sort by actual rating
                : products.OrderByDescending(p => p.Id).ToList(),

            "newest" => ascending
                ? products.OrderBy(p => p.CreatedAt).ToList()
                : products.OrderByDescending(p => p.CreatedAt).ToList(),

            _ => products.OrderByDescending(p => p.CreatedAt).ToList() // Default: newest
        };
    }

    private async Task<ProductSearchResultDto> MapToSearchResultDto(Product product)
    {
        var stats = await _reviewService.GetProductReviewStatsAsync(product.Id);
        decimal? discountPercent = null;
        if (product.SalePrice.HasValue && product.BasePrice > 0)
        {
            discountPercent = (decimal)(((double)(product.BasePrice - product.SalePrice.Value) / (double)product.BasePrice) * 100);
        }

        return new ProductSearchResultDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            SKU = product.SKU,
            ShortDescription = product.ShortDescription,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            SupplierId = product.SupplierId,
            SupplierName = product.Supplier?.Name,
            BasePrice = product.BasePrice,
            SalePrice = product.SalePrice,
            StockQuantity = product.StockQuantity,
            ReviewCount = stats.TotalReviews,
            AverageRating = (decimal)stats.AverageRating,
            IsFeatured = product.IsFeatured,
            IsOnSale = product.SalePrice.HasValue && product.SalePrice < product.BasePrice,
            DiscountPercent = discountPercent,
            ImageUrl = product.Images?.FirstOrDefault()?.ImageUrl,
            CreatedAt = product.CreatedAt
        };
    }
}
