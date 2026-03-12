using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChinhNha.Infrastructure.Services;

public class AppUserService : IAppUserService
{
    private readonly AppDbContext _context;

    public AppUserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetUserByIdAsync(string userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<AppUser?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> UpdateUserAsync(AppUser user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
