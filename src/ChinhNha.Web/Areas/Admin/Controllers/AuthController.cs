using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IAuditService auditService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            return RedirectToAction("Index", "Dashboard");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.LoginAsync(model.Email, model.Password, model.RememberMe);
        if (result.Succeeded)
        {
            if (result.Roles.Contains("Admin"))
            {
                await TryLogAuditAsync(
                    userId: result.UserId,
                    action: "Login",
                    entityType: "Auth",
                    description: $"Admin login success: {model.Email}",
                    isSuccessful: true);

                return RedirectToAction("Index", "Dashboard");
            }

            await TryLogAuditAsync(
                userId: result.UserId,
                action: "LoginDenied",
                entityType: "Auth",
                description: $"Login denied (not Admin): {model.Email}",
                isSuccessful: false,
                errorMessage: "Tai khoan khong co quyen Admin.");

            // Đăng nhập thành công nhưng không phải Admin -> đăng xuất ngay
            await _authService.SignOutAsync();
            ModelState.AddModelError(string.Empty, "Tài khoản này không có quyền truy cập trang quản trị.");
            return View(model);
        }

        await TryLogAuditAsync(
            userId: result.UserId,
            action: "LoginFailed",
            entityType: "Auth",
            description: $"Admin login failed: {model.Email}",
            isSuccessful: false,
            errorMessage: result.ErrorMessage ?? "Email hoac mat khau khong chinh xac.");

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Email hoặc mật khẩu không chính xác.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await TryLogAuditAsync(
            userId: currentUserId,
            action: "Logout",
            entityType: "Auth",
            description: "Admin logout",
            isSuccessful: true);

        await _authService.SignOutAsync();
        return RedirectToAction("Login");
    }

    private async Task TryLogAuditAsync(
        string? userId,
        string action,
        string entityType,
        string description,
        bool isSuccessful,
        string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        try
        {
            await _auditService.LogActionAsync(
                userId: userId,
                action: action,
                entityType: entityType,
                entityId: null,
                description: description,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                isSuccessful: isSuccessful,
                errorMessage: errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the ghi audit log cho action {Action}.", action);
        }
    }
}
