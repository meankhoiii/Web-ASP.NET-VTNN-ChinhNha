using ChinhNha.Domain.Entities;

namespace ChinhNha.Application.Interfaces;

public interface IAppUserService
{
    Task<AppUser?> GetUserByIdAsync(string userId);
    Task<AppUser?> GetUserByEmailAsync(string email);
    Task<bool> UpdateUserAsync(AppUser user);
}
