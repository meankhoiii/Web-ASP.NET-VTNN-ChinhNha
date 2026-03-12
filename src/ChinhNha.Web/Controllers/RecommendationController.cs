using System.Security.Claims;
using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationController : ControllerBase
{
    private readonly IProductRecommendationService _recommendationService;
    private readonly ILogger<RecommendationController> _logger;

    public RecommendationController(
        IProductRecommendationService recommendationService,
        ILogger<RecommendationController> logger)
    {
        _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Authorize]
    [HttpGet("for-user")]
    public async Task<ActionResult<RecommendationsListDto>> GetForUser([FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var result = await _recommendationService.GetRecommendationsAsync(userId, NormalizeLimit(limit));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for current user");
            return BadRequest(new { message = "Lỗi khi lấy gợi ý sản phẩm" });
        }
    }

    [HttpGet("similar/{productId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<SimilarProductsDto>> GetSimilarProducts(int productId, [FromQuery] int limit = 5)
    {
        try
        {
            if (productId <= 0)
                return BadRequest(new { message = "Sản phẩm không hợp lệ" });

            var result = await _recommendationService.GetSimilarProductsAsync(productId, NormalizeLimit(limit, 1, 20));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar products for product {ProductId}", productId);
            return BadRequest(new { message = "Lỗi khi lấy sản phẩm tương tự", error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("generate")]
    public async Task<ActionResult<IEnumerable<ProductRecommendationDto>>> Generate(
        [FromQuery] RecommendationStrategy strategy = RecommendationStrategy.Category,
        [FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var result = await _recommendationService.GenerateRecommendationsAsync(userId, strategy, NormalizeLimit(limit));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations with strategy {Strategy}", strategy);
            return BadRequest(new { message = "Lỗi khi sinh gợi ý sản phẩm", error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("personalized")]
    public async Task<ActionResult<IEnumerable<ProductRecommendationDto>>> GetPersonalized([FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var result = await _recommendationService.GetPersonalizedRecommendationsAsync(userId, NormalizeLimit(limit));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personalized recommendations");
            return BadRequest(new { message = "Lỗi khi lấy gợi ý cá nhân hóa" });
        }
    }

    [Authorize]
    [HttpGet("trending")]
    public async Task<ActionResult<IEnumerable<ProductRecommendationDto>>> GetTrending([FromQuery] int limit = 5)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var result = await _recommendationService.GetTrendingRecommendationsAsync(userId, NormalizeLimit(limit, 1, 20));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending recommendations");
            return BadRequest(new { message = "Lỗi khi lấy gợi ý thịnh hành" });
        }
    }

    [AllowAnonymous]
    [HttpGet("frequently-bought-together/{productId:int}")]
    public async Task<ActionResult<IEnumerable<ProductRecommendationDto>>> GetFrequentlyBoughtTogether(
        int productId,
        [FromQuery] int limit = 5)
    {
        try
        {
            if (productId <= 0)
                return BadRequest(new { message = "Sản phẩm không hợp lệ" });

            var result = await _recommendationService.GetFrequentlyBoughtTogetherAsync(productId, NormalizeLimit(limit, 1, 20));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting frequently bought together for product {ProductId}", productId);
            return BadRequest(new { message = "Lỗi khi lấy gợi ý mua kèm", error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("{recommendationId:int}/interaction")]
    public async Task<ActionResult> RecordInteraction(int recommendationId, [FromBody] RecommendationInteractionRequest request)
    {
        try
        {
            if (recommendationId <= 0)
                return BadRequest(new { message = "Recommendation không hợp lệ" });

            if (string.IsNullOrWhiteSpace(request.InteractionType))
                return BadRequest(new { message = "Interaction type là bắt buộc" });

            await _recommendationService.RecordInteractionAsync(recommendationId, request.InteractionType);
            return Ok(new { message = "Đã ghi nhận tương tác" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording interaction for recommendation {RecommendationId}", recommendationId);
            return BadRequest(new { message = "Lỗi khi ghi nhận tương tác", error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("metrics")]
    public async Task<ActionResult> GetMetrics([FromQuery] int days = 30)
    {
        try
        {
            var safeDays = Math.Clamp(days, 1, 365);
            var metrics = await _recommendationService.GetRecommendationMetricsAsync(safeDays);
            return Ok(new
            {
                days = safeDays,
                clickThroughRate = metrics.ClickThroughRate,
                conversionRate = metrics.ConversionRate,
                averageScore = metrics.AverageScore
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendation metrics");
            return BadRequest(new { message = "Lỗi khi lấy chỉ số recommendation" });
        }
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirst("sub")?.Value;
    }

    private static int NormalizeLimit(int value, int min = 1, int max = 50)
    {
        return Math.Clamp(value, min, max);
    }

    public class RecommendationInteractionRequest
    {
        public string InteractionType { get; set; } = string.Empty;
    }
}
