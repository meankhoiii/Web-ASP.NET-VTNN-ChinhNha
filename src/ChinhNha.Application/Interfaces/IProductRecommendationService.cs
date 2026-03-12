using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Application.Interfaces;

/// <summary>Service for product recommendations</summary>
public interface IProductRecommendationService
{
    /// <summary>Get recommendations for user</summary>
    Task<RecommendationsListDto> GetRecommendationsAsync(string userId, int limit = 10);

    /// <summary>Get similar products</summary>
    Task<SimilarProductsDto> GetSimilarProductsAsync(int productId, int limit = 5);

    /// <summary>Generate recommendations based on strategy</summary>
    Task<IEnumerable<ProductRecommendationDto>> GenerateRecommendationsAsync(
        string userId, 
        RecommendationStrategy strategy, 
        int limit = 10);

    /// <summary>Record recommendation interaction</summary>
    Task RecordInteractionAsync(int recommendationId, string interactionType);

    /// <summary>Get recommendation effectiveness metrics</summary>
    Task<(double ClickThroughRate, double ConversionRate, int AverageScore)> GetRecommendationMetricsAsync(int days = 30);

    /// <summary>Get personalized recommendations based on history</summary>
    Task<IEnumerable<ProductRecommendationDto>> GetPersonalizedRecommendationsAsync(string userId, int limit = 10);

    /// <summary>Get trending recommendations</summary>
    Task<IEnumerable<ProductRecommendationDto>> GetTrendingRecommendationsAsync(string userId, int limit = 5);

    /// <summary>Get frequently bought together recommendations</summary>
    Task<IEnumerable<ProductRecommendationDto>> GetFrequentlyBoughtTogetherAsync(int productId, int limit = 5);
}
