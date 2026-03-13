namespace ChinhNha.Web.Areas.Admin.Models;

public class AiSettingsViewModel
{
    public bool AutoSelectEnabled { get; set; }
    public string? ManualModel { get; set; }
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    public bool UseGeminiForUser { get; set; }
    public string GeminiModel { get; set; } = "gemini-2.5-flash";
    public string? GeminiApiKey { get; set; }
    public bool HasGeminiApiKey { get; set; }

    public double DetectedRamGb { get; set; }
    public int CpuCores { get; set; }
    public bool HasNvidiaGpu { get; set; }
    public bool OllamaReachable { get; set; }

    public IReadOnlyList<string> InstalledModels { get; set; } = Array.Empty<string>();
    public string RecommendedModel { get; set; } = "Sailor2-8B-Chat";
    public string EffectiveModel { get; set; } = "Sailor2-8B-Chat";

    public IReadOnlyList<string> SupportedModels { get; set; } = new[]
    {
        "Sailor2-20B-Chat",
        "Sailor2-8B-Chat",
        "Sailor2-1B-Chat",
        "qwen3:8b",
        "qwen3:4b",
        "qwen3:1.7b",
        "llama3.1:8b",
        "llama3.2:3b",
        "phi4",
        "phi4-mini"
    };

    public IReadOnlyList<string> GeminiModels { get; set; } = new[]
    {
        "gemini-2.5-flash",
        "gemini-2.5-pro",
        "gemini-2.0-flash",
        "gemini-2.0-flash-lite",
        "gemini-1.5-flash",
        "gemini-1.5-pro"
    };
}
