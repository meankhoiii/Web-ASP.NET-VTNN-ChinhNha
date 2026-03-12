using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> Index(int days = 30, string? previewUserId = null, int previewLimit = 8)
    {
        var safeDays = Math.Clamp(days, 1, 365);
        var safePreviewLimit = Math.Clamp(previewLimit, 1, 20);

        var model = new RecommendationAnalyticsViewModel
        {
            Days = safeDays,
            PreviewUserId = string.IsNullOrWhiteSpace(previewUserId) ? null : previewUserId,
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
                    .ToDictionary(g => g.Key, g => g.Count());
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recommendation analytics");
            TempData["Error"] = "Khong the tai thong ke recommendation. Vui long thu lai.";
            return View(model);
        }
    }
}
