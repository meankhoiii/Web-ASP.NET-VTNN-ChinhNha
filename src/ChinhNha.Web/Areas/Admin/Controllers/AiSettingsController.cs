using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AiSettingsController : Controller
{
    private readonly IAiModelSelectionService _aiModelSelectionService;

    public AiSettingsController(IAiModelSelectionService aiModelSelectionService)
    {
        _aiModelSelectionService = aiModelSelectionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await _aiModelSelectionService.GetSettingsAsync();
        var model = new AiSettingsViewModel
        {
            AutoSelectEnabled = settings.AutoSelectEnabled,
            ManualModel = settings.ManualModel,
            OllamaEndpoint = settings.OllamaEndpoint,
            DetectedRamGb = settings.DetectedRamGb,
            CpuCores = settings.CpuCores,
            HasNvidiaGpu = settings.HasNvidiaGpu,
            OllamaReachable = settings.OllamaReachable,
            InstalledModels = settings.InstalledModels,
            RecommendedModel = settings.RecommendedModel,
            EffectiveModel = settings.EffectiveModel
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AiSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var fallback = await _aiModelSelectionService.GetSettingsAsync();
            model.DetectedRamGb = fallback.DetectedRamGb;
            model.CpuCores = fallback.CpuCores;
            model.HasNvidiaGpu = fallback.HasNvidiaGpu;
            model.OllamaReachable = fallback.OllamaReachable;
            model.InstalledModels = fallback.InstalledModels;
            model.RecommendedModel = fallback.RecommendedModel;
            model.EffectiveModel = fallback.EffectiveModel;
            return View(model);
        }

        await _aiModelSelectionService.SaveSettingsAsync(model.AutoSelectEnabled, model.ManualModel, model.OllamaEndpoint);
        TempData["SuccessMessage"] = "Đã cập nhật cấu hình mô hình AI.";

        return RedirectToAction(nameof(Index));
    }
}
