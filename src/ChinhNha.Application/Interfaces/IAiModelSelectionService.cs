using ChinhNha.Application.DTOs.Requests;

namespace ChinhNha.Application.Interfaces;

public interface IAiModelSelectionService
{
    Task<AiModelSettingsDto> GetSettingsAsync();
    Task SaveSettingsAsync(
        bool autoSelectEnabled,
        string? manualModel,
        string? ollamaEndpoint = null,
        bool? useGeminiForUser = null,
        string? geminiModel = null,
        string? geminiApiKey = null);
    Task<bool> EnsureModelInstalledAsync(string modelName, string? ollamaEndpoint = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AiModelInstallProgressDto> EnsureModelInstalledWithProgressAsync(
        string modelName,
        string? ollamaEndpoint = null,
        CancellationToken cancellationToken = default);
    Task<string> GetEffectiveModelAsync();
    Task<string?> GetGeminiApiKeyAsync();
}
