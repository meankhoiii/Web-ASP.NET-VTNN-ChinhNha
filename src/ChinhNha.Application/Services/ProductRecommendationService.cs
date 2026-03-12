using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace ChinhNha.Application.Services;

/// <summary>Product recommendation service implementation</summary>
public class ProductRecommendationService : IProductRecommendationService
{
    private readonly IProductRecommendationRepository _recommendationRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductReviewService _reviewService;
    private readonly IOrderRepository _orderRepository;
    private readonly ISearchAnalyticsRepository _searchAnalyticsRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductRecommendationService> _logger;

    public ProductRecommendationService(
        IProductRecommendationRepository recommendationRepository,
        IProductRepository productRepository,
        IProductReviewService reviewService,
        IOrderRepository orderRepository,
        ISearchAnalyticsRepository searchAnalyticsRepository,
        IMapper mapper,
        ILogger<ProductRecommendationService> logger)
    {
        _recommendationRepository = recommendationRepository ?? throw new ArgumentNullException(nameof(recommendationRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _searchAnalyticsRepository = searchAnalyticsRepository ?? throw new ArgumentNullException(nameof(searchAnalyticsRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Get recommendations for user</summary>
    public async Task<RecommendationsListDto> GetRecommendationsAsync(string userId, int limit = 10)
    {
        try
        {
            var recommendations = await _recommendationRepository.GetUserRecommendationsAsync(userId, limit);
            var dtos = await MapToDtosAsync(recommendations);

            var stats = await _recommendationRepository.GetRecommendationStatsAsync(userId, 30);
            
            double ctr = stats.TotalShown > 0 ? (double)stats.Clicked / stats.TotalShown * 100 : 0;
            double conversionRate = stats.TotalShown > 0 ? (double)stats.Purchases / stats.TotalShown * 100 : 0;
            int avgScore = recommendations.Any() ? (int)recommendations.Average(r => r.RecommendationScore) : 0;

            return new RecommendationsListDto
            {
                Recommendations = dtos,
                TotalCount = recommendations.Count(),
                ClickThroughRate = ctr,
                ConversionRate = conversionRate,
                AverageRecommendationScore = avgScore
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting recommendations for user {userId}");
            throw;
        }
    }

    /// <summary>Get similar products</summary>
    public async Task<SimilarProductsDto> GetSimilarProductsAsync(int productId, int limit = 5)
    {
        try
        {
            var mainProduct = await _productRepository.GetByIdAsync(productId);
            if (mainProduct == null)
                throw new Exception("Product not found");

            var allProducts = await _productRepository.ListAllAsync();
            var similar = new List<(Product Product, int Score)>();

            // Find products with same category
            var sameCategory = allProducts
                .Where(p => p.Id != productId && p.CategoryId == mainProduct.CategoryId)
                .ToList();

            // Score by rating and popularity
            foreach (var product in sameCategory)
            {
                int score = 50; // Base score for same category
                var stats = await _reviewService.GetProductReviewStatsAsync(product.Id);
                score += (int)(stats.AverageRating * 5); // Add rating score
                score += Math.Min(stats.TotalReviews / 5, 20); // Add review count
                similar.Add((product, score));
            }

            // Also add same supplier products with lower weight
            var sameSupplier = allProducts
                .Where(p => p.Id != productId && p.SupplierId == mainProduct.SupplierId)
                .Take(limit)
                .ToList();

            foreach (var product in sameSupplier)
            {
                if (!similar.Any(s => s.Product.Id == product.Id))
                {
                    int score = 30; // Lower score for same supplier
                    var stats = await _reviewService.GetProductReviewStatsAsync(product.Id);
                    score += (int)(stats.AverageRating * 3);
                    similar.Add((product, score));
                }
            }

            var topSimilar = similar
                .OrderByDescending(s => s.Score)
                .Take(limit)
                .Select(s => s.Product)
                .ToList();

            var searchResultDtos = new List<ProductSearchResultDto>();
            foreach (var product in topSimilar)
            {
                var stats = await _reviewService.GetProductReviewStatsAsync(product.Id);
                searchResultDtos.Add(new ProductSearchResultDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Slug = product.Slug,
                    BasePrice = product.BasePrice,
                    SalePrice = product.SalePrice,
                    ImageUrl = product.Images?.FirstOrDefault()?.ImageUrl,
                    AverageRating = (decimal)stats.AverageRating,
                    ReviewCount = stats.TotalReviews,
                    CategoryName = product.Category?.Name,
                    IsOnSale = product.SalePrice.HasValue && product.SalePrice < product.BasePrice
                });
            }

            return new SimilarProductsDto
            {
                ProductId = productId,
                ProductName = mainProduct.Name,
                SimilarProducts = searchResultDtos,
                SimilarityReason = "Based on category and supplier"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting similar products for {productId}");
            throw;
        }
    }

    /// <summary>Generate recommendations based on strategy</summary>
    public async Task<IEnumerable<ProductRecommendationDto>> GenerateRecommendationsAsync(
        string userId,
        RecommendationStrategy strategy,
        int limit = 10)
    {
        try
        {
            var recommendations = new List<ProductRecommendation>();

            switch (strategy)
            {
                case RecommendationStrategy.Category:
                    recommendations = await GenerateCategoryBasedAsync(userId, limit);
                    break;
                case RecommendationStrategy.Trending:
                    recommendations = await GenerateTrendingAsync(userId, limit);
                    break;
                case RecommendationStrategy.TopRated:
                    recommendations = await GenerateTopRatedAsync(userId, limit);
                    break;
                case RecommendationStrategy.NewArrivals:
                    recommendations = await GenerateNewArrivalsAsync(userId, limit);
                    break;
                case RecommendationStrategy.OnSale:
                    recommendations = await GenerateOnSaleAsync(userId, limit);
                    break;
                default:
                    recommendations = await GenerateCategoryBasedAsync(userId, limit);
                    break;
            }

            await _recommendationRepository.SaveRecommendationsAsync(recommendations);
            return await MapToDtosAsync(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating recommendations for user {userId} with strategy {strategy}");
            throw;
        }
    }

    /// <summary>Record recommendation interaction</summary>
    public async Task RecordInteractionAsync(int recommendationId, string interactionType)
    {
        try
        {
            switch (interactionType.ToLower())
            {
                case "view":
                    await _recommendationRepository.MarkAsViewedAsync(recommendationId);
                    break;
                case "click":
                    await _recommendationRepository.MarkAsClickedAsync(recommendationId);
                    break;
                case "cart":
                    await _recommendationRepository.MarkAsAddedToCartAsync(recommendationId);
                    break;
                case "purchase":
                    await _recommendationRepository.MarkAsPurchasedAsync(recommendationId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error recording interaction {interactionType} for recommendation {recommendationId}");
            throw;
        }
    }

    /// <summary>Get recommendation effectiveness metrics</summary>
    public async Task<(double ClickThroughRate, double ConversionRate, int AverageScore)> GetRecommendationMetricsAsync(int days = 30)
    {
        try
        {
            var stats = await _recommendationRepository.GetRecommendationStatsAsync(null, days);
            
            double ctr = stats.TotalShown > 0 ? (double)stats.Clicked / stats.TotalShown * 100 : 0;
            double conversionRate = stats.TotalShown > 0 ? (double)stats.Purchases / stats.TotalShown * 100 : 0;

            return (ctr, conversionRate, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendation metrics");
            throw;
        }
    }

    /// <summary>Get personalized recommendations based on history</summary>
    public async Task<IEnumerable<ProductRecommendationDto>> GetPersonalizedRecommendationsAsync(string userId, int limit = 10)
    {
        return await GenerateRecommendationsAsync(userId, RecommendationStrategy.Category, limit);
    }

    /// <summary>Get trending recommendations</summary>
    public async Task<IEnumerable<ProductRecommendationDto>> GetTrendingRecommendationsAsync(string userId, int limit = 5)
    {
        return await GenerateRecommendationsAsync(userId, RecommendationStrategy.Trending, limit);
    }

    /// <summary>Get frequently bought together recommendations</summary>
    public async Task<IEnumerable<ProductRecommendationDto>> GetFrequentlyBoughtTogetherAsync(int productId, int limit = 5)
    {
        // Simplified: returns same-category products as frequently bought together
        var mainProduct = await _productRepository.GetByIdAsync(productId);
        if (mainProduct == null)
            return new List<ProductRecommendationDto>();

        var similar = await GetSimilarProductsAsync(productId, limit);
        var result = new List<ProductRecommendationDto>();

        foreach (var prod in similar.SimilarProducts)
        {
            result.Add(new ProductRecommendationDto
            {
                RecommendedProductId = prod.Id,
                RecommendedProductName = prod.Name,
                RecommendedProductImage = prod.ImageUrl,
                RecommendedProductPrice = prod.BasePrice,
                RecommendedProductSalePrice = prod.SalePrice,
                RecommendedProductRating = prod.AverageRating,
                RecommendationReason = "Frequently bought together",
                RecommendationType = "ProductDetail",
                RecommendationScore = 75
            });
        }

        return result;
    }

    // Helper methods for recommendation strategies
    private async Task<List<ProductRecommendation>> GenerateCategoryBasedAsync(string userId, int limit)
    {
        var allProducts = await _productRepository.ListAllAsync();
        var recommendations = new List<ProductRecommendation>();

        var topProducts = allProducts
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToList();

        foreach (var product in topProducts)
        {
            recommendations.Add(new ProductRecommendation
            {
                UserId = userId,
                RecommendedProductId = product.Id,
                RecommendationReason = "Category",
                RecommendationScore = 50,
                RecommendationType = "Homepage"
            });
        }

        return recommendations;
    }

    private async Task<List<ProductRecommendation>> GenerateTrendingAsync(string userId, int limit)
    {
        var mostRecommended = await _recommendationRepository.GetMostRecommendedProductsAsync(limit, 30);
        var recommendations = new List<ProductRecommendation>();

        foreach (var (productId, count) in mostRecommended)
        {
            recommendations.Add(new ProductRecommendation
            {
                UserId = userId,
                RecommendedProductId = productId,
                RecommendationReason = "Trending",
                RecommendationScore = Math.Min(count * 10, 100),
                RecommendationType = "Homepage"
            });
        }

        return recommendations;
    }

    private async Task<List<ProductRecommendation>> GenerateTopRatedAsync(string userId, int limit)
    {
        var allProducts = await _productRepository.ListAllAsync();
        var recommendations = new List<ProductRecommendation>();

        var topRated = new List<(Product Product, double Rating)>();
        foreach (var product in allProducts)
        {
            var stats = await _reviewService.GetProductReviewStatsAsync(product.Id);
            if (stats.TotalReviews > 0)
            {
                topRated.Add((product, stats.AverageRating));
            }
        }

        var sorted = topRated.OrderByDescending(x => x.Rating).Take(limit);
        foreach (var (product, rating) in sorted)
        {
            recommendations.Add(new ProductRecommendation
            {
                UserId = userId,
                RecommendedProductId = product.Id,
                RecommendationReason = "TopRated",
                RecommendationScore = (int)(rating * 20),
                RecommendationType = "Homepage"
            });
        }

        return recommendations;
    }

    private async Task<List<ProductRecommendation>> GenerateNewArrivalsAsync(string userId, int limit)
    {
        var allProducts = await _productRepository.ListAllAsync();
        var recommendations = new List<ProductRecommendation>();

        var newest = allProducts
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToList();

        foreach (var product in newest)
        {
            recommendations.Add(new ProductRecommendation
            {
                UserId = userId,
                RecommendedProductId = product.Id,
                RecommendationReason = "NewArrivals",
                RecommendationScore = 60,
                RecommendationType = "Homepage"
            });
        }

        return recommendations;
    }

    private async Task<List<ProductRecommendation>> GenerateOnSaleAsync(string userId, int limit)
    {
        var allProducts = await _productRepository.ListAllAsync();
        var recommendations = new List<ProductRecommendation>();

        var onSale = allProducts
            .Where(p => p.SalePrice.HasValue && p.SalePrice < p.BasePrice)
            .OrderByDescending(p => (p.BasePrice - p.SalePrice) / p.BasePrice * 100) // Highest discount %
            .Take(limit)
            .ToList();

        foreach (var product in onSale)
        {
            var discountPercent = (product.BasePrice - product.SalePrice!.Value) / product.BasePrice * 100;
            recommendations.Add(new ProductRecommendation
            {
                UserId = userId,
                RecommendedProductId = product.Id,
                RecommendationReason = "OnSale",
                RecommendationScore = (int)Math.Min(discountPercent, 100),
                RecommendationType = "Homepage"
            });
        }

        return recommendations;
    }

    private async Task<IEnumerable<ProductRecommendationDto>> MapToDtosAsync(IEnumerable<ProductRecommendation> recommendations)
    {
        var result = new List<ProductRecommendationDto>();

        foreach (var rec in recommendations)
        {
            var product = await _productRepository.GetByIdAsync(rec.RecommendedProductId);
            if (product != null)
            {
                var stats = await _reviewService.GetProductReviewStatsAsync(product.Id);
                result.Add(new ProductRecommendationDto
                {
                    Id = rec.Id,
                    RecommendedProductId = rec.RecommendedProductId,
                    RecommendedProductName = product.Name,
                    RecommendedProductImage = product.Images?.FirstOrDefault()?.ImageUrl,
                    RecommendedProductPrice = product.BasePrice,
                    RecommendedProductSalePrice = product.SalePrice,
                    RecommendedProductRating = (decimal)stats.AverageRating,
                    RecommendationReason = rec.RecommendationReason,
                    RecommendationScore = rec.RecommendationScore,
                    RecommendationType = rec.RecommendationType,
                    IsViewed = rec.ViewedAt.HasValue,
                    IsClicked = rec.IsClicked,
                    IsAddedToCart = rec.IsAddedToCart,
                    IsPurchased = rec.IsPurchased,
                    CreatedAt = rec.CreatedAt
                });
            }
        }

        return result;
    }
}
