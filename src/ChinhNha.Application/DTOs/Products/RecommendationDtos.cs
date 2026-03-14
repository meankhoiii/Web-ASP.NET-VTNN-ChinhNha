namespace ChinhNha.Application.DTOs.Products;

/// <summary>Product recommendation DTO</summary>
public class ProductRecommendationDto
{
    public int Id { get; set; }
    public int RecommendedProductId { get; set; }
    public string RecommendedProductName { get; set; } = string.Empty;
    public string? RecommendedProductSlug { get; set; }
    public string? RecommendedProductImage { get; set; }
    public decimal RecommendedProductPrice { get; set; }
    public decimal? RecommendedProductSalePrice { get; set; }
    public decimal? RecommendedProductRating { get; set; }
    
    public int? RelatedProductId { get; set; }
    public string? RelatedProductName { get; set; }
    
    public string RecommendationReason { get; set; } = string.Empty;
    public int RecommendationScore { get; set; }
    public string RecommendationType { get; set; } = string.Empty;
    
    public bool IsViewed { get; set; }
    public bool IsClicked { get; set; }
    public bool IsAddedToCart { get; set; }
    public bool IsPurchased { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

/// <summary>Recommendations list with stats</summary>
public class RecommendationsListDto
{
    public IEnumerable<ProductRecommendationDto> Recommendations { get; set; } = new List<ProductRecommendationDto>();
    public int TotalCount { get; set; }
    public double ClickThroughRate { get; set; } // Clicked / Shown %
    public double ConversionRate { get; set; } // Purchased / Shown %
    public int AverageRecommendationScore { get; set; }
}

/// <summary>Similar products result</summary>
public class SimilarProductsDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public IEnumerable<ProductSearchResultDto> SimilarProducts { get; set; } = new List<ProductSearchResultDto>();
    public string SimilarityReason { get; set; } = string.Empty;
}

/// <summary>Recommendation strategy options</summary>
public enum RecommendationStrategy
{
    /// <summary>Based on same category</summary>
    Category = 0,
    
    /// <summary>Based on search history</summary>
    SearchBased = 1,
    
    /// <summary>Based on browse history</summary>
    BrowsingHistory = 2,
    
    /// <summary>Based on same supplier</summary>
    SupplierBased = 3,
    
    /// <summary>Trending products</summary>
    Trending = 4,
    
    /// <summary>Average highest rated products</summary>
    TopRated = 5,
    
    /// <summary>On sale/discounted products</summary>
    OnSale = 6,
    
    /// <summary>New arrivals</summary>
    NewArrivals = 7,
    
    /// <summary>Customers who bought X also bought Y</summary>
    CollaborativeFiltering = 8
}