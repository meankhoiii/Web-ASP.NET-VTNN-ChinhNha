namespace ChinhNha.Application.DTOs.Requests;

public class AuthResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
