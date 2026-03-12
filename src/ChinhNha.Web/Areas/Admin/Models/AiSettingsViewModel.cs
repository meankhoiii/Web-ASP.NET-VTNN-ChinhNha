namespace ChinhNha.Web.Areas.Admin.Models;

public class AiSettingsViewModel
{
    public bool AutoSelectEnabled { get; set; }
    public string? ManualModel { get; set; }
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    public double DetectedRamGb { get; set; }
    public int CpuCores { get; set; }
    public bool HasNvidiaGpu { get; set; }
    public bool OllamaReachable { get; set; }

    public IReadOnlyList<string> InstalledModels { get; set; } = Array.Empty<string>();
    public string RecommendedModel { get; set; } = "Sailor2-8B-Chat";
    public string EffectiveModel { get; set; } = "Sailor2-8B-Chat";

    public IReadOnlyList<string> SupportedModels { get; set; } = new[]
    {
        // Sailor2 is prioritized for chat in this project.
        "Sailor2-20B-Chat",
        "Sailor2-8B-Chat",
        "Sailor2-1B-Chat",
        // Qwen3
        "qwen3:8b",
        "qwen3:4b",
        "qwen3:1.7b",
        // Llama3
        "llama3.1:8b",
        "llama3.2:3b",
        // Phi-4
        "phi4",
        "phi4-mini"
    };
}
