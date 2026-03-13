using ChinhNha.Application.DTOs.Requests;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ChinhNha.Infrastructure.Services;

public class AiModelSelectionService : IAiModelSelectionService
{
    private const string AiGroup = "AI";
    private const string AutoSelectKey = "AI_AUTO_SELECT";
    private const string ManualModelKey = "AI_MANUAL_MODEL";
    private const string OllamaEndpointKey = "AI_OLLAMA_ENDPOINT";
    private const string UseGeminiForUserKey = "AI_USE_GEMINI_FOR_USER";
    private const string GeminiModelKey = "AI_GEMINI_MODEL";
    private const string GeminiApiKeyProtectedKey = "AI_GEMINI_API_KEY_PROTECTED";

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

    private static readonly Dictionary<string, string> ModelToOllamaTag =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sailor2-1B-Chat"] = "sailor2:1b",
            ["Sailor2-8B-Chat"] = "sailor2:8b",
            ["Sailor2-20B-Chat"] = "sailor2:20b",
            ["qwen3:8b"] = "qwen3:8b",
            ["qwen3:4b"] = "qwen3:4b",
            ["qwen3:1.7b"] = "qwen3:1.7b",
            ["llama3.1:8b"] = "llama3.1:8b",
            ["llama3.2:3b"] = "llama3.2:3b",
            ["phi4"] = "phi4",
            ["phi4-mini"] = "phi4-mini"
        };

    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataProtector _secretProtector;

    public AiModelSelectionService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _secretProtector = dataProtectionProvider.CreateProtector("ChinhNha.AI.GeminiApiKey");
    }

    public async Task<AiModelSettingsDto> GetSettingsAsync()
    {
        var autoSelect = await GetBoolSettingAsync(AutoSelectKey, true);
        var manualModel = await GetStringSettingAsync(ManualModelKey);
        var endpoint = await GetStringSettingAsync(OllamaEndpointKey) ?? "http://localhost:11434";
        var useGeminiForUser = await GetBoolSettingAsync(UseGeminiForUserKey, false);
        var geminiModel = await GetStringSettingAsync(GeminiModelKey) ?? "gemini-2.5-flash";
        var protectedGeminiKey = await GetStringSettingAsync(GeminiApiKeyProtectedKey);

        var hardware = DetectHardware();
        var ollama = await GetOllamaModelsAsync(endpoint);

        var recommended = GetRecommendedModel(hardware.DetectedRamGb, hardware.CpuCores, hardware.HasNvidiaGpu);
        var effective = ResolveEffectiveModel(autoSelect, manualModel, recommended, ollama.InstalledModels);

        return new AiModelSettingsDto
        {
            AutoSelectEnabled = autoSelect,
            ManualModel = manualModel,
            OllamaEndpoint = endpoint,
            UseGeminiForUser = useGeminiForUser,
            GeminiModel = geminiModel,
            HasGeminiApiKey = !string.IsNullOrWhiteSpace(protectedGeminiKey),
            DetectedRamGb = hardware.DetectedRamGb,
            CpuCores = hardware.CpuCores,
            HasNvidiaGpu = hardware.HasNvidiaGpu,
            OllamaReachable = ollama.Reachable,
            InstalledModels = ollama.InstalledModels,
            RecommendedModel = recommended,
            EffectiveModel = effective
        };
    }

    public async Task SaveSettingsAsync(
        bool autoSelectEnabled,
        string? manualModel,
        string? ollamaEndpoint = null,
        bool? useGeminiForUser = null,
        string? geminiModel = null,
        string? geminiApiKey = null)
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

        if (useGeminiForUser.HasValue)
        {
            await UpsertSettingAsync(UseGeminiForUserKey, useGeminiForUser.Value ? "true" : "false");
        }

        if (!string.IsNullOrWhiteSpace(geminiModel))
        {
            await UpsertSettingAsync(GeminiModelKey, geminiModel.Trim());
        }

        if (!string.IsNullOrWhiteSpace(geminiApiKey))
        {
            var protectedKey = _secretProtector.Protect(geminiApiKey.Trim());
            await UpsertSettingAsync(GeminiApiKeyProtectedKey, protectedKey);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> EnsureModelInstalledAsync(string modelName, string? ollamaEndpoint = null, CancellationToken cancellationToken = default)
    {
        if (!ModelToOllamaTag.TryGetValue(modelName, out var modelTag))
        {
            return false;
        }

        var endpoint = string.IsNullOrWhiteSpace(ollamaEndpoint)
            ? await GetStringSettingAsync(OllamaEndpointKey) ?? "http://localhost:11434"
            : ollamaEndpoint.Trim();

        var ollamaModels = await GetOllamaRawNamesAsync(endpoint);
        if (ollamaModels.Reachable && ollamaModels.RawNames.Any(name => name.StartsWith(modelTag, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return await PullModelAsync(endpoint, modelTag, cancellationToken);
    }

    public async IAsyncEnumerable<AiModelInstallProgressDto> EnsureModelInstalledWithProgressAsync(
        string modelName,
        string? ollamaEndpoint = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!ModelToOllamaTag.TryGetValue(modelName, out var modelTag))
        {
            yield return new AiModelInstallProgressDto
            {
                Message = $"Model '{modelName}' không hợp lệ.",
                IsError = true,
                IsCompleted = true
            };
            yield break;
        }

        var endpoint = string.IsNullOrWhiteSpace(ollamaEndpoint)
            ? await GetStringSettingAsync(OllamaEndpointKey) ?? "http://localhost:11434"
            : ollamaEndpoint.Trim();

        yield return new AiModelInstallProgressDto
        {
            Message = $"Đang kiểm tra model '{modelName}'...",
            Percent = 5
        };

        var ollamaModels = await GetOllamaRawNamesAsync(endpoint);
        if (ollamaModels.Reachable && ollamaModels.RawNames.Any(name => name.StartsWith(modelTag, StringComparison.OrdinalIgnoreCase)))
        {
            yield return new AiModelInstallProgressDto
            {
                Message = $"Model '{modelName}' đã có sẵn trong Ollama.",
                Percent = 100,
                IsCompleted = true
            };
            yield break;
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(20);

        var payload = JsonSerializer.Serialize(new
        {
            name = modelTag,
            stream = true
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint.TrimEnd('/')}/api/pull")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            yield return new AiModelInstallProgressDto
            {
                Message = $"Không thể pull model '{modelName}' (HTTP {(int)response.StatusCode}).",
                IsError = true,
                IsCompleted = true
            };
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var statusEl)
                ? statusEl.GetString() ?? "Đang xử lý..."
                : "Đang tải model...";

            int? percent = null;
            if (root.TryGetProperty("total", out var totalEl) &&
                root.TryGetProperty("completed", out var completedEl) &&
                totalEl.ValueKind == JsonValueKind.Number &&
                completedEl.ValueKind == JsonValueKind.Number)
            {
                var total = totalEl.GetDouble();
                var completed = completedEl.GetDouble();
                if (total > 0)
                {
                    percent = (int)Math.Clamp(Math.Round(completed / total * 100), 0, 100);
                }
            }

            var done = status.Contains("success", StringComparison.OrdinalIgnoreCase);
            yield return new AiModelInstallProgressDto
            {
                Message = status,
                Percent = percent,
                IsCompleted = done,
                IsError = status.Contains("error", StringComparison.OrdinalIgnoreCase)
            };

            if (done)
            {
                yield break;
            }
        }

        yield return new AiModelInstallProgressDto
        {
            Message = $"Hoàn tất pull model '{modelName}'.",
            Percent = 100,
            IsCompleted = true
        };
    }

    public async Task<string> GetEffectiveModelAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.EffectiveModel;
    }

    public async Task<string?> GetGeminiApiKeyAsync()
    {
        var protectedKey = await GetStringSettingAsync(GeminiApiKeyProtectedKey);
        if (string.IsNullOrWhiteSpace(protectedKey))
        {
            return null;
        }

        try
        {
            return _secretProtector.Unprotect(protectedKey);
        }
        catch
        {
            return null;
        }
    }

    private static string GetRecommendedModel(double ramGb, int cpuCores, bool hasNvidiaGpu)
    {
        // Sailor2 is prioritized for the chat experience in this project.
        if (ramGb >= 18 && (hasNvidiaGpu || cpuCores >= 8))
        {
            return "Sailor2-20B-Chat";
        }

        if (ramGb >= 10 && (hasNvidiaGpu || cpuCores >= 6))
        {
            return "Sailor2-8B-Chat";
        }

        return "Sailor2-1B-Chat";
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

        // Fallback order: prefer Sailor2 first, then Qwen3, then Llama3, then Phi-4.
        var fallbackOrder = new[]
        {
            "Sailor2-20B-Chat", "Sailor2-8B-Chat", "Sailor2-1B-Chat",
            "qwen3:8b", "qwen3:4b", "qwen3:1.7b",
            "llama3.1:8b", "llama3.2:3b",
            "phi4", "phi4-mini"
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
            client.Timeout = TimeSpan.FromSeconds(10);

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

                // Map Ollama tags → display names dùng trong app.
                if (name.StartsWith("sailor2:1b",  StringComparison.OrdinalIgnoreCase)) installed.Add("Sailor2-1B-Chat");
                if (name.StartsWith("sailor2:8b",  StringComparison.OrdinalIgnoreCase)) installed.Add("Sailor2-8B-Chat");
                if (name.StartsWith("sailor2:20b", StringComparison.OrdinalIgnoreCase)) installed.Add("Sailor2-20B-Chat");
                if (name.StartsWith("qwen3:8b",    StringComparison.OrdinalIgnoreCase)) installed.Add("qwen3:8b");
                if (name.StartsWith("qwen3:4b",    StringComparison.OrdinalIgnoreCase)) installed.Add("qwen3:4b");
                if (name.StartsWith("qwen3:1.7b",  StringComparison.OrdinalIgnoreCase)) installed.Add("qwen3:1.7b");
                if (name.StartsWith("llama3.1:8b", StringComparison.OrdinalIgnoreCase)) installed.Add("llama3.1:8b");
                if (name.StartsWith("llama3.2:3b", StringComparison.OrdinalIgnoreCase)) installed.Add("llama3.2:3b");
                if (name.StartsWith("phi4-mini",   StringComparison.OrdinalIgnoreCase)) installed.Add("phi4-mini");
                else if (name.StartsWith("phi4",   StringComparison.OrdinalIgnoreCase)) installed.Add("phi4");
            }

            return (true, installed.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }
        catch
        {
            return (false, Array.Empty<string>());
        }
    }

    private async Task<(bool Reachable, IReadOnlyList<string> RawNames)> GetOllamaRawNamesAsync(string endpoint)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

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

            var rawNames = new List<string>();
            foreach (var model in modelsElement.EnumerateArray())
            {
                if (!model.TryGetProperty("name", out var nameElement))
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    rawNames.Add(name);
                }
            }

            return (true, rawNames);
        }
        catch
        {
            return (false, Array.Empty<string>());
        }
    }

    private async Task<bool> PullModelAsync(string endpoint, string modelTag, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            var payload = JsonSerializer.Serialize(new
            {
                name = modelTag,
                stream = false
            });

            using var response = await client.PostAsync(
                $"{endpoint.TrimEnd('/')}/api/pull",
                new StringContent(payload, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }
        catch
        {
            // Fallback to CLI below.
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("ollama", $"pull {modelTag}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
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
