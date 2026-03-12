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
    public string RecommendedModel { get; set; } = "Sailor2-1B-Chat";
    public string EffectiveModel { get; set; } = "Sailor2-1B-Chat";

    public IReadOnlyList<string> SupportedModels { get; set; } = new[]
    {
        "Sailor2-1B-Chat",
        "Sailor2-8B-Chat",
        "Sailor2-20B-Chat"
    };
}
