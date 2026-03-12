using ChinhNha.Application.DTOs.Requests;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChinhNha.Infrastructure.Services;

public class CookieAuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPasswordHashService _passwordHashService;

    public CookieAuthService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IPasswordHashService passwordHashService)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _passwordHashService = passwordHashService;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null)
        {
            return new AuthResult { Succeeded = false, ErrorMessage = "Email hoặc mật khẩu không chính xác." };
        }

        var isValidPassword = _passwordHashService.VerifyPassword(password, user.PasswordHash);
        if (!isValidPassword)
        {
            return new AuthResult { Succeeded = false, ErrorMessage = "Email hoặc mật khẩu không chính xác." };
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();
        await SignInInternalAsync(user, roles, rememberMe);

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new AuthResult
        {
            Succeeded = true,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles
        };
    }

    public async Task<AuthResult> RegisterAsync(string fullName, string email, string password, string roleName = "Customer")
    {
        var existing = await _context.Users.AnyAsync(u => u.Email == email);
        if (existing)
        {
            return new AuthResult { Succeeded = false, ErrorMessage = "Email đã tồn tại." };
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
        {
            role = new Role { Name = roleName };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        var user = new AppUser
        {
            Email = email,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHashService.HashPassword(password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(new AppUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await _context.SaveChangesAsync();

        var roles = new List<string> { role.Name };
        await SignInInternalAsync(user, roles, false);

        return new AuthResult
        {
            Succeeded = true,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles
        };
    }

    public async Task SignOutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId)
    {
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToListAsync();

        return roles;
    }

    private async Task SignInInternalAsync(AppUser user, IReadOnlyList<string> roles, bool rememberMe)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext not available for sign-in.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new("full_name", user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null,
                AllowRefresh = true
            });
    }
}
