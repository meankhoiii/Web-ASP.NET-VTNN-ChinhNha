using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class RecommendationAnalyticsController : Controller
{
    private readonly IProductRecommendationService _recommendationService;
    private readonly ILogger<RecommendationAnalyticsController> _logger;

    public RecommendationAnalyticsController(
        IProductRecommendationService recommendationService,
        ILogger<RecommendationAnalyticsController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int days = 30, int previewLimit = 8)
    {
        var safeDays = Math.Clamp(days, 1, 365);
        var safePreviewLimit = Math.Clamp(previewLimit, 1, 20);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var model = new RecommendationAnalyticsViewModel
        {
            Days = safeDays,
            PreviewUserId = currentUserId,
            PreviewLimit = safePreviewLimit
        };

        try
        {
            var metrics = await _recommendationService.GetRecommendationMetricsAsync(safeDays);
            model.ClickThroughRate = metrics.ClickThroughRate;
            model.ConversionRate = metrics.ConversionRate;
            model.AverageScore = metrics.AverageScore;

            if (!string.IsNullOrWhiteSpace(model.PreviewUserId))
            {
                var personalized = await _recommendationService.GetPersonalizedRecommendationsAsync(model.PreviewUserId, safePreviewLimit);
                var trending = await _recommendationService.GetTrendingRecommendationsAsync(model.PreviewUserId, safePreviewLimit);

                model.PersonalizedRecommendations = personalized.ToList();
                model.TrendingRecommendations = trending.ToList();

                model.ReasonDistribution = model.PersonalizedRecommendations
                    .Concat(model.TrendingRecommendations)
                    .GroupBy(x => x.RecommendationReason)
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => TranslateReason(g.Key), g => g.Count());
            }
            else
            {
                TempData["Error"] = "Không xác định được tài khoản đăng nhập để tạo dữ liệu gợi ý.";
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi tai trang phan tich goi y san pham");
            TempData["Error"] = "Không thể tải thống kê gợi ý sản phẩm. Vui lòng thử lại.";
            return View(model);
        }
    }

    private static string TranslateReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "Khác";
        }

        return reason.Trim() switch
        {
            "Category" => "Cùng danh mục",
            "Trending" => "Xu hướng",
            "TopRated" => "Đánh giá cao",
            "NewArrivals" => "Hàng mới",
            "OnSale" => "Đang giảm giá",
            "FrequentlyBoughtTogether" => "Thường mua kèm",
            _ => reason
        };
    }
}
