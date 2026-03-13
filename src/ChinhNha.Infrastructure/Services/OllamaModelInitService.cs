using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ChinhNha.Application.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ChinhNha.Infrastructure.Services;

/// <summary>
/// Background service chạy khi app khởi động.
/// Nếu Ollama đang chạy trên máy nhưng chưa có model mặc định,
/// service sẽ tự động gọi `ollama pull` để tải về.
/// </summary>
public class OllamaModelInitService : BackgroundService
{
    // Map từ display name (dùng trong app) → Ollama tag (dùng khi pull)
    private static readonly Dictionary<string, string> ModelToOllamaTag =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sailor2-1B-Chat"]  = "sailor2:1b",
            ["Sailor2-8B-Chat"]  = "sailor2:8b",
            ["Sailor2-20B-Chat"] = "sailor2:20b",
            ["qwen3:8b"]         = "qwen3:8b",
            ["qwen3:4b"]         = "qwen3:4b",
            ["qwen3:1.7b"]       = "qwen3:1.7b",
            ["llama3.1:8b"]      = "llama3.1:8b",
            ["llama3.2:3b"]      = "llama3.2:3b",
            ["phi4"]             = "phi4",
            ["phi4-mini"]        = "phi4-mini",
        };

    private readonly ILogger<OllamaModelInitService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly string _ollamaEndpoint;

    public OllamaModelInitService(
        ILogger<OllamaModelInitService> logger,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _serviceScopeFactory = serviceScopeFactory;
        _ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Chờ app hoàn tất khởi động trước khi thực hiện
            await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
            if (stoppingToken.IsCancellationRequested) return;

            _logger.LogInformation("[OllamaInit] Đang kiểm tra Ollama tại {Endpoint}...", _ollamaEndpoint);

            var (reachable, rawNames) = await GetOllamaRawNamesAsync(stoppingToken);
            if (!reachable)
            {
                _logger.LogInformation("[OllamaInit] Ollama chưa chạy — bỏ qua tự động pull.");
                return;
            }

            var recommendedModel = GetRecommendedModel();
            _logger.LogInformation("[OllamaInit] Model gợi ý cho phần cứng này: {Model}", recommendedModel);

            if (!ModelToOllamaTag.TryGetValue(recommendedModel, out var ollamaTag))
            {
                _logger.LogWarning("[OllamaInit] Không tìm thấy Ollama tag cho model '{Model}'.", recommendedModel);
                return;
            }

            // Kiểm tra model đã được cài chưa (so sánh theo prefix của tag)
            bool alreadyInstalled = rawNames.Any(n =>
                n.StartsWith(ollamaTag, StringComparison.OrdinalIgnoreCase));

            if (alreadyInstalled)
            {
                _logger.LogInformation("[OllamaInit] Model '{Tag}' đã được cài sẵn. Không cần pull.", ollamaTag);
            }

            if (!alreadyInstalled)
            {
                _logger.LogInformation("[OllamaInit] Chưa tìm thấy '{Tag}'. Bắt đầu tự động tải (có thể mất vài phút)...", ollamaTag);
                await PullModelAsync(ollamaTag, stoppingToken);

                // Refresh model list after pull
                var refreshed = await GetOllamaRawNamesAsync(stoppingToken);
                rawNames = refreshed.RawNames;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var aiModelSelectionService = scope.ServiceProvider.GetRequiredService<IAiModelSelectionService>();

            await EnsureModelUsableOrDowngradeAsync(recommendedModel, rawNames, aiModelSelectionService, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[OllamaInit] Tiến trình khởi tạo model bị hủy do ứng dụng dừng.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OllamaInit] Lỗi ngoài ý muốn khi khởi tạo model. Bỏ qua để không ảnh hưởng vòng đời ứng dụng.");
        }
    }

    private async Task EnsureModelUsableOrDowngradeAsync(
        string startDisplayModel,
        IReadOnlyList<string> installedRawNames,
        IAiModelSelectionService aiModelSelectionService,
        CancellationToken ct)
    {
        var currentModel = startDisplayModel;
        var knownModels = installedRawNames.ToList();

        while (!ct.IsCancellationRequested)
        {
            if (!ModelToOllamaTag.TryGetValue(currentModel, out var currentTag))
            {
                _logger.LogWarning("[OllamaInit] Không tìm thấy tag cho model '{Model}'. Dừng kiểm tra.", currentModel);
                return;
            }

            if (!knownModels.Any(name => name.StartsWith(currentTag, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("[OllamaInit] Model '{Tag}' chưa cài. Tự động pull...", currentTag);
                await PullModelAsync(currentTag, ct);

                var refreshed = await GetOllamaRawNamesAsync(ct);
                knownModels = refreshed.RawNames;
            }

            var testResult = await TestModelAsync(currentTag, ct);
            if (testResult == ModelTestResult.Ok)
            {
                _logger.LogInformation("[OllamaInit] Model '{Model}' hoạt động bình thường.", currentModel);
                return;
            }

            if (testResult != ModelTestResult.InsufficientMemory)
            {
                _logger.LogWarning("[OllamaInit] Test model '{Model}' thất bại (không phải lỗi RAM). Giữ nguyên cấu hình hiện tại.", currentModel);
                return;
            }

            var smallerModel = GetSmallerSailorModel(currentModel);
            if (smallerModel == null)
            {
                _logger.LogWarning("[OllamaInit] Model '{Model}' thiếu RAM và không còn model Sailor nhỏ hơn để fallback.", currentModel);
                return;
            }

            _logger.LogWarning(
                "[OllamaInit] Model '{CurrentModel}' thiếu RAM. Tự động fallback xuống '{SmallerModel}'.",
                currentModel,
                smallerModel);

            await aiModelSelectionService.SaveSettingsAsync(
                autoSelectEnabled: false,
                manualModel: smallerModel,
                ollamaEndpoint: _ollamaEndpoint);

            currentModel = smallerModel;
        }
    }

    private async Task<ModelTestResult> TestModelAsync(string ollamaTag, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(20);

            var payload = JsonSerializer.Serialize(new
            {
                model = ollamaTag,
                messages = new[] { new { role = "user", content = "ping" } },
                stream = false
            });

            using var response = await client.PostAsync(
                $"{_ollamaEndpoint.TrimEnd('/')}/api/chat",
                new StringContent(payload, Encoding.UTF8, "application/json"),
                ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
            {
                return ModelTestResult.Ok;
            }

            if (body.Contains("requires more system memory", StringComparison.OrdinalIgnoreCase))
            {
                return ModelTestResult.InsufficientMemory;
            }

            _logger.LogWarning("[OllamaInit] Test model '{Tag}' trả về HTTP {Status}: {Body}", ollamaTag, (int)response.StatusCode, body);
            return ModelTestResult.OtherError;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("[OllamaInit] Test model '{Tag}' bị timeout.", ollamaTag);
            return ModelTestResult.OtherError;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OllamaInit] Lỗi khi test model '{Tag}'.", ollamaTag);
            return ModelTestResult.OtherError;
        }
    }

    private static string? GetSmallerSailorModel(string currentModel)
    {
        return currentModel switch
        {
            "Sailor2-20B-Chat" => "Sailor2-8B-Chat",
            "Sailor2-8B-Chat" => "Sailor2-1B-Chat",
            _ => null
        };
    }

    private enum ModelTestResult
    {
        Ok,
        InsufficientMemory,
        OtherError
    }

    /// <summary>
    /// Xác định model phù hợp dựa trên RAM và CPU của máy.
    /// </summary>
    private static string GetRecommendedModel()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var ramBytes = gcInfo.TotalAvailableMemoryBytes;
        if (ramBytes <= 0) ramBytes = Environment.WorkingSet;
        var ramGb = ramBytes / 1024d / 1024d / 1024d;
        var cpuCores = Environment.ProcessorCount;

        bool hasNvidiaGpu = false;
        try
        {
            using var p = new Process();
            p.StartInfo = new ProcessStartInfo("nvidia-smi", "--query-gpu=name --format=csv,noheader")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            if (p.Start() && p.WaitForExit(1500) && p.ExitCode == 0)
                hasNvidiaGpu = !string.IsNullOrWhiteSpace(p.StandardOutput.ReadToEnd());
        }
        catch { /* nvidia-smi không có trên máy */ }

        if (ramGb >= 18 && (hasNvidiaGpu || cpuCores >= 8)) return "Sailor2-20B-Chat";
        if (ramGb >= 10 && (hasNvidiaGpu || cpuCores >= 6)) return "Sailor2-8B-Chat";
        return "Sailor2-1B-Chat";
    }

    /// <summary>
    /// Lấy danh sách tên model thô từ Ollama API /api/tags.
    /// </summary>
    private async Task<(bool Reachable, List<string> RawNames)> GetOllamaRawNamesAsync(CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(8);

            using var response = await client.GetAsync($"{_ollamaEndpoint.TrimEnd('/')}/api/tags", ct);
            if (!response.IsSuccessStatusCode) return (false, []);

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("models", out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
                return (true, []);

            var names = new List<string>();
            foreach (var model in arr.EnumerateArray())
            {
                if (!model.TryGetProperty("name", out var n)) continue;
                var name = n.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }

            return (true, names);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug("[OllamaInit] Không thể kết nối Ollama: {Msg}", ex.Message);
            return (false, []);
        }
    }

    /// <summary>
    /// Chạy `ollama pull <tag>` và log tiến trình.
    /// </summary>
    private async Task PullModelAsync(string ollamaTag, CancellationToken ct)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("ollama", $"pull {ollamaTag}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    _logger.LogInformation("[OllamaInit] {Line}", e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    _logger.LogWarning("[OllamaInit] {Line}", e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
                _logger.LogInformation("[OllamaInit] Đã tải xong model '{Tag}'.", ollamaTag);
            else
                _logger.LogWarning("[OllamaInit] Pull '{Tag}' kết thúc với exit code {Code}.", ollamaTag, process.ExitCode);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[OllamaInit] Quá trình pull bị hủy do app đang tắt.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OllamaInit] Lỗi khi pull model '{Tag}'. Hãy tự chạy: ollama pull {Tag}", ollamaTag, ollamaTag);
        }
    }
}
