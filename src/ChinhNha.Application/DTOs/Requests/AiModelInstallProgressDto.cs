namespace ChinhNha.Application.DTOs.Requests;

public class AiModelInstallProgressDto
{
    public string Message { get; set; } = string.Empty;
    public int? Percent { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsError { get; set; }
}
