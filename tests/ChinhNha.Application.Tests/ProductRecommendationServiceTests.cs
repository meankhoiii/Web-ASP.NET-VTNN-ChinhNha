using AutoMapper;
using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using ChinhNha.Application.Services;
using ChinhNha.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChinhNha.Application.Tests;

public class ProductRecommendationServiceTests
{
    private readonly Mock<IProductRecommendationRepository> _recommendationRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IProductReviewService> _reviewService = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ISearchAnalyticsRepository> _searchAnalyticsRepository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ILogger<ProductRecommendationService>> _logger = new();

    [Theory]
    [InlineData("view")]
    [InlineData("click")]
    [InlineData("cart")]
    [InlineData("purchase")]
    public async Task RecordInteractionAsync_DelegatesToExpectedRepositoryMethod(string interactionType)
    {
        var service = CreateService();

        await service.RecordInteractionAsync(15, interactionType);

        _recommendationRepository.Verify(repo => repo.MarkAsViewedAsync(15), interactionType == "view" ? Times.Once : Times.Never);
        _recommendationRepository.Verify(repo => repo.MarkAsClickedAsync(15), interactionType == "click" ? Times.Once : Times.Never);
        _recommendationRepository.Verify(repo => repo.MarkAsAddedToCartAsync(15), interactionType == "cart" ? Times.Once : Times.Never);
        _recommendationRepository.Verify(repo => repo.MarkAsPurchasedAsync(15), interactionType == "purchase" ? Times.Once : Times.Never);
    }

    [Fact]
    public async Task GetRecommendationMetricsAsync_ComputesRatesFromStats()
    {
        _recommendationRepository
            .Setup(repo => repo.GetRecommendationStatsAsync(null, 30))
            .ReturnsAsync((200, 150, 50, 18, 10));

        var service = CreateService();

        var result = await service.GetRecommendationMetricsAsync();

        Assert.Equal(25d, result.ClickThroughRate);
        Assert.Equal(5d, result.ConversionRate);
        Assert.Equal(0, result.AverageScore);
    }

    private ProductRecommendationService CreateService()
    {
        return new ProductRecommendationService(
            _recommendationRepository.Object,
            _productRepository.Object,
            _reviewService.Object,
            _orderRepository.Object,
            _searchAnalyticsRepository.Object,
            _mapper.Object,
            _logger.Object);
    }
}