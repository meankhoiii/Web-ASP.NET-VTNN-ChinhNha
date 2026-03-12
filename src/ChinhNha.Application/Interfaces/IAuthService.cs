using ChinhNha.Application.DTOs.Requests;

namespace ChinhNha.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, bool rememberMe);
    Task<AuthResult> RegisterAsync(string fullName, string email, string password, string roleName = "Customer");
    Task SignOutAsync();
    Task<IReadOnlyList<string>> GetRolesAsync(string userId);
}
