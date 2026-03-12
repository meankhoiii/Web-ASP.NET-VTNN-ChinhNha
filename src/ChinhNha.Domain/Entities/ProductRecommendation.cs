namespace ChinhNha.Domain.Entities;

/// <summary>
/// Tracks product recommendations and recommendation effectiveness
/// </summary>
public class ProductRecommendation : BaseEntity
{
    /// <summary>User receiving the recommendation</summary>
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    /// <summary>Primary product being recommended</summary>
    public int RecommendedProductId { get; set; }
    public Product RecommendedProduct { get; set; } = null!;

    /// <summary>Related product (if recommendation is for similar products)</summary>
    public int? RelatedProductId { get; set; }
    public Product? RelatedProduct { get; set; }

    /// <summary>Reason for recommendation: Similar, Category, Trending, Search, View, Purchase</summary>
    public string RecommendationReason { get; set; } = string.Empty;

    /// <summary>Recommendation score/confidence (0-100)</summary>
    public int RecommendationScore { get; set; }

    /// <summary>Type: ProductDetail, Search, Homepage, Email, Notification</summary>
    public string RecommendationType { get; set; } = "ProductDetail";

    /// <summary>Was recommendation shown to user</summary>
    public bool IsShown { get; set; } = true;

    /// <summary>Date recommendation was generated</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Date user viewed the recommendation</summary>
    public DateTime? ViewedAt { get; set; }

    /// <summary>Did user click on recommendation</summary>
    public bool IsClicked { get; set; }

    /// <summary>Date user clicked</summary>
    public DateTime? ClickedAt { get; set; }

    /// <summary>Did user add to cart after recommendation</summary>
    public bool IsAddedToCart { get; set; }

    /// <summary>Did user purchase after recommendation</summary>
    public bool IsPurchased { get; set; }

    /// <summary>Date of purchase</summary>
    public DateTime? PurchasedAt { get; set; }

    /// <summary>Conversion value: 0 (no action), 1 (view), 2 (cart), 3 (purchase)</summary>
    public int ConversionValue { get; set; } = 0;
}
