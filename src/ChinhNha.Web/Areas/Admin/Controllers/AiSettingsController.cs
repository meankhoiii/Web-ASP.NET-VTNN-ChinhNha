using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
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
            UseGeminiForUser = settings.UseGeminiForUser,
            GeminiModel = settings.GeminiModel,
            HasGeminiApiKey = settings.HasGeminiApiKey,
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
            model.UseGeminiForUser = fallback.UseGeminiForUser;
            model.GeminiModel = fallback.GeminiModel;
            model.HasGeminiApiKey = fallback.HasGeminiApiKey;
            model.DetectedRamGb = fallback.DetectedRamGb;
            model.CpuCores = fallback.CpuCores;
            model.HasNvidiaGpu = fallback.HasNvidiaGpu;
            model.OllamaReachable = fallback.OllamaReachable;
            model.InstalledModels = fallback.InstalledModels;
            model.RecommendedModel = fallback.RecommendedModel;
            model.EffectiveModel = fallback.EffectiveModel;
            return View(model);
        }

        await _aiModelSelectionService.SaveSettingsAsync(
            model.AutoSelectEnabled,
            model.ManualModel,
            model.OllamaEndpoint,
            model.UseGeminiForUser,
            model.GeminiModel,
            model.GeminiApiKey);

        var pullStatusMessage = string.Empty;
        if (!model.AutoSelectEnabled && !string.IsNullOrWhiteSpace(model.ManualModel))
        {
            var installed = await _aiModelSelectionService.EnsureModelInstalledAsync(model.ManualModel, model.OllamaEndpoint);
            pullStatusMessage = installed
                ? $" Model '{model.ManualModel}' đã sẵn sàng trong Ollama."
                : $" Không thể tự pull model '{model.ManualModel}'. Hãy kiểm tra Ollama rồi thử lại.";
        }

        TempData["SuccessMessage"] = $"Đã cập nhật cấu hình mô hình AI.{pullStatusMessage}";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Admin/AiSettings/SaveStream")]
    public async Task SaveStream([FromBody] AiSettingsViewModel model)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        if (!TryValidateModel(model))
        {
            await WriteNdjsonAsync(new { type = "error", message = "Dữ liệu cấu hình không hợp lệ." });
            await WriteNdjsonAsync(new { type = "done" });
            return;
        }

        try
        {
            await WriteNdjsonAsync(new { type = "status", message = "Đang lưu cấu hình AI...", progress = 10 });

            await _aiModelSelectionService.SaveSettingsAsync(
                model.AutoSelectEnabled,
                model.ManualModel,
                model.OllamaEndpoint,
                model.UseGeminiForUser,
                model.GeminiModel,
                model.GeminiApiKey);

            await WriteNdjsonAsync(new { type = "status", message = "Đã lưu cấu hình thành công.", progress = 35 });

            if (!model.AutoSelectEnabled && !string.IsNullOrWhiteSpace(model.ManualModel))
            {
                await WriteNdjsonAsync(new { type = "status", message = $"Đang kiểm tra model '{model.ManualModel}'...", progress = 45 });

                await foreach (var progress in _aiModelSelectionService.EnsureModelInstalledWithProgressAsync(
                    model.ManualModel,
                    model.OllamaEndpoint,
                    HttpContext.RequestAborted))
                {
                    await WriteNdjsonAsync(new
                    {
                        type = progress.IsError ? "warning" : "status",
                        message = progress.Message,
                        progress = progress.Percent
                    });
                }

                await WriteNdjsonAsync(new
                {
                    type = "success",
                    message = $"Đã cập nhật cấu hình và hoàn tất xử lý model '{model.ManualModel}'.",
                    progress = 100
                });
            }
            else
            {
                await WriteNdjsonAsync(new
                {
                    type = "success",
                    message = "Đã cập nhật cấu hình AI thành công.",
                    progress = 100
                });
            }
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected.
        }
        catch (Exception ex)
        {
            await WriteNdjsonAsync(new { type = "error", message = "Lưu cấu hình thất bại.", detail = ex.Message });
        }
        finally
        {
            await WriteNdjsonAsync(new { type = "done" });
        }

        async Task WriteNdjsonAsync(object payload)
        {
            var line = JsonSerializer.Serialize(payload);
            await Response.WriteAsync(line + "\n");
            await Response.Body.FlushAsync();
        }
    }
}
