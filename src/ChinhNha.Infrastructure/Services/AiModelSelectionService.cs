using ChinhNha.Application.DTOs.Requests;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace ChinhNha.Infrastructure.Services;

public class AiModelSelectionService : IAiModelSelectionService
{
    private const string AiGroup = "AI";
    private const string AutoSelectKey = "AI_AUTO_SELECT";
    private const string ManualModelKey = "AI_MANUAL_MODEL";
    private const string OllamaEndpointKey = "AI_OLLAMA_ENDPOINT";

    private static readonly string[] AllowedModels =
    {
        // Sailor2 series
        "Sailor2-1B-Chat",
        "Sailor2-8B-Chat",
        "Sailor2-20B-Chat",
        // Qwen3 (tiếng Việt tốt, recommended by design doc)
        "qwen3:8b",
        "qwen3:4b",
        "qwen3:1.7b",
        // Llama3 (OpenAI-compatible function calling)
        "llama3.1:8b",
        "llama3.2:3b",
        // Phi-4
        "phi4",
        "phi4-mini"
    };

    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiModelSelectionService(AppDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AiModelSettingsDto> GetSettingsAsync()
    {
        var autoSelect = await GetBoolSettingAsync(AutoSelectKey, true);
        var manualModel = await GetStringSettingAsync(ManualModelKey);
        var endpoint = await GetStringSettingAsync(OllamaEndpointKey) ?? "http://localhost:11434";

        var hardware = DetectHardware();
        var ollama = await GetOllamaModelsAsync(endpoint);

        var recommended = GetRecommendedModel(hardware.DetectedRamGb, hardware.CpuCores, hardware.HasNvidiaGpu);
        var effective = ResolveEffectiveModel(autoSelect, manualModel, recommended, ollama.InstalledModels);

        return new AiModelSettingsDto
        {
            AutoSelectEnabled = autoSelect,
            ManualModel = manualModel,
            OllamaEndpoint = endpoint,
            DetectedRamGb = hardware.DetectedRamGb,
            CpuCores = hardware.CpuCores,
            HasNvidiaGpu = hardware.HasNvidiaGpu,
            OllamaReachable = ollama.Reachable,
            InstalledModels = ollama.InstalledModels,
            RecommendedModel = recommended,
            EffectiveModel = effective
        };
    }

    public async Task SaveSettingsAsync(bool autoSelectEnabled, string? manualModel, string? ollamaEndpoint = null)
    {
        if (!string.IsNullOrWhiteSpace(manualModel) && !AllowedModels.Contains(manualModel))
        {
            throw new ArgumentException("Model không hợp lệ.", nameof(manualModel));
        }

        await UpsertSettingAsync(AutoSelectKey, autoSelectEnabled ? "true" : "false");
        await UpsertSettingAsync(ManualModelKey, string.IsNullOrWhiteSpace(manualModel) ? null : manualModel);

        if (!string.IsNullOrWhiteSpace(ollamaEndpoint))
        {
            await UpsertSettingAsync(OllamaEndpointKey, ollamaEndpoint.Trim());
        }

        await _context.SaveChangesAsync();
    }

    public async Task<string> GetEffectiveModelAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.EffectiveModel;
    }

    private static string GetRecommendedModel(double ramGb, int cpuCores, bool hasNvidiaGpu)
    {
        // Qwen3 preferred for Vietnamese language quality (per design doc)
        if (ramGb >= 10 && (hasNvidiaGpu || cpuCores >= 6))
        {
            return "qwen3:8b";
        }

        if (ramGb >= 6)
        {
            return "qwen3:4b";
        }

        return "llama3.2:3b";
    }

    private static string ResolveEffectiveModel(bool autoSelect, string? manualModel, string recommendedModel, IReadOnlyList<string> installedModels)
    {
        if (!autoSelect && !string.IsNullOrWhiteSpace(manualModel))
        {
            return manualModel;
        }

        if (installedModels.Count == 0)
        {
            return recommendedModel;
        }

        if (installedModels.Contains(recommendedModel, StringComparer.OrdinalIgnoreCase))
        {
            return recommendedModel;
        }

        // Fallback order: prefer Qwen3, then Llama3, then Sailor2
        var fallbackOrder = new[]
        {
            "qwen3:8b", "qwen3:4b", "qwen3:1.7b",
            "llama3.1:8b", "llama3.2:3b",
            "Sailor2-20B-Chat", "Sailor2-8B-Chat", "Sailor2-1B-Chat"
        };
        var fallback = fallbackOrder.FirstOrDefault(m => installedModels.Contains(m, StringComparer.OrdinalIgnoreCase));

        return fallback ?? installedModels[0];
    }

    private (double DetectedRamGb, int CpuCores, bool HasNvidiaGpu) DetectHardware()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var available = gcInfo.TotalAvailableMemoryBytes;

        if (available <= 0 || available == long.MaxValue)
        {
            available = Environment.WorkingSet;
        }

        var ramGb = Math.Round(available / 1024d / 1024d / 1024d, 1);
        var cpuCores = Environment.ProcessorCount;
        var hasNvidiaGpu = TryDetectNvidiaGpu();

        return (ramGb, cpuCores, hasNvidiaGpu);
    }

    private static bool TryDetectNvidiaGpu()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "nvidia-smi";
            process.StartInfo.Arguments = "--query-gpu=name --format=csv,noheader";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            if (!process.Start())
            {
                return false;
            }

            if (!process.WaitForExit(1000))
            {
                try { process.Kill(); } catch { }
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            return false;
        }
    }

    private async Task<(bool Reachable, IReadOnlyList<string> InstalledModels)> GetOllamaModelsAsync(string endpoint)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(2);

            using var response = await client.GetAsync($"{endpoint.TrimEnd('/')}/api/tags");
            if (!response.IsSuccessStatusCode)
            {
                return (false, Array.Empty<string>());
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("models", out var modelsElement) || modelsElement.ValueKind != JsonValueKind.Array)
            {
                return (true, Array.Empty<string>());
            }

            var installed = new List<string>();
            foreach (var model in modelsElement.EnumerateArray())
            {
                if (!model.TryGetProperty("name", out var nameElement))
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                // Map common Ollama tags to requested display names.
                if (name.StartsWith("sailor2:1b", StringComparison.OrdinalIgnoreCase)) installed.Add("Sailor2-1B-Chat");
                if (name.StartsWith("sailor2:8b", StringComparison.OrdinalIgnoreCase)) installed.Add("Sailor2-8B-Chat");
                if (name.StartsWith("sailor2:20b", StringComparison.OrdinalIgnoreCase)) installed.Add("Sailor2-20B-Chat");
            }

            return (true, installed.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }
        catch
        {
            return (false, Array.Empty<string>());
        }
    }

    private async Task<bool> GetBoolSettingAsync(string key, bool defaultValue)
    {
        var text = await GetStringSettingAsync(key);
        return bool.TryParse(text, out var value) ? value : defaultValue;
    }

    private async Task<string?> GetStringSettingAsync(string key)
    {
        return await _context.SiteSettings
            .Where(s => s.Group == AiGroup && s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
    }

    private async Task UpsertSettingAsync(string key, string? value)
    {
        var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Group == AiGroup && s.Key == key);
        if (setting == null)
        {
            _context.SiteSettings.Add(new SiteSettings
            {
                Group = AiGroup,
                Key = key,
                Value = value
            });
            return;
        }

        setting.Value = value;
    }
}
