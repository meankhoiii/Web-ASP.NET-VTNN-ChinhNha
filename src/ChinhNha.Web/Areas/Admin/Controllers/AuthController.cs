using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
                return RedirectToAction("Index", "Dashboard");

            // Đăng nhập thành công nhưng không phải Admin -> đăng xuất ngay
            await _authService.SignOutAsync();
            ModelState.AddModelError(string.Empty, "Tài khoản này không có quyền truy cập trang quản trị.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Email hoặc mật khẩu không chính xác.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync();
        return RedirectToAction("Login");
    }
}
