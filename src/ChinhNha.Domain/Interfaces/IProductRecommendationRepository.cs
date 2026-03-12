using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

/// <summary>Repository for product recommendations</summary>
public interface IProductRecommendationRepository : IRepository<ProductRecommendation>
{
    /// <summary>Get recommendations for a user</summary>
    Task<IEnumerable<ProductRecommendation>> GetUserRecommendationsAsync(string userId, int limit = 10);

    /// <summary>Get recommendations by type</summary>
    Task<IEnumerable<ProductRecommendation>> GetRecommendationsByTypeAsync(string userId, string recommendationType, int limit = 10);

    /// <summary>Mark recommendation as viewed</summary>
    Task MarkAsViewedAsync(int recommendationId);

    /// <summary>Mark recommendation as clicked</summary>
    Task MarkAsClickedAsync(int recommendationId);

    /// <summary>Mark recommendation as added to cart</summary>
    Task MarkAsAddedToCartAsync(int recommendationId);

    /// <summary>Mark recommendation as purchased</summary>
    Task MarkAsPurchasedAsync(int recommendationId);

    /// <summary>Save new recommendation(s)</summary>
    Task SaveRecommendationsAsync(IEnumerable<ProductRecommendation> recommendations);

    /// <summary>Get recommendation effectiveness stats</summary>
    Task<(int TotalShown, int Viewed, int Clicked, int CartAdds, int Purchases)> GetRecommendationStatsAsync(string? userId = null, int days = 30);

    /// <summary>Get recommendations by reason</summary>
    Task<IEnumerable<ProductRecommendation>> GetRecommendationsByReasonAsync(string reason, int limit = 10);

    /// <summary>Delete old recommendations</summary>
    Task<int> DeleteOldRecommendationsAsync(int daysOld = 90);

    /// <summary>Get products frequently recommended</summary>
    Task<IEnumerable<(int ProductId, int RecommendationCount)>> GetMostRecommendedProductsAsync(int limit = 10, int days = 30);
}
