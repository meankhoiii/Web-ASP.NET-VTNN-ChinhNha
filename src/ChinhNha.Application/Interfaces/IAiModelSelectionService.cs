using ChinhNha.Application.DTOs.Requests;

namespace ChinhNha.Application.Interfaces;

public interface IAiModelSelectionService
{
    Task<AiModelSettingsDto> GetSettingsAsync();
    Task SaveSettingsAsync(bool autoSelectEnabled, string? manualModel, string? ollamaEndpoint = null);
    Task<string> GetEffectiveModelAsync();
}
