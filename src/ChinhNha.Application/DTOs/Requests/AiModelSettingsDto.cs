namespace ChinhNha.Application.DTOs.Requests;

public class AiModelSettingsDto
{
    public bool AutoSelectEnabled { get; set; }
    public string? ManualModel { get; set; }
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    public bool UseGeminiForUser { get; set; }
    public string GeminiModel { get; set; } = "gemini-2.5-flash";
    public bool HasGeminiApiKey { get; set; }

    public double DetectedRamGb { get; set; }
    public int CpuCores { get; set; }
    public bool HasNvidiaGpu { get; set; }
    public bool OllamaReachable { get; set; }

    public IReadOnlyList<string> InstalledModels { get; set; } = Array.Empty<string>();
    public string RecommendedModel { get; set; } = "Sailor2-8B-Chat";
    public string EffectiveModel { get; set; } = "Sailor2-8B-Chat";
}
