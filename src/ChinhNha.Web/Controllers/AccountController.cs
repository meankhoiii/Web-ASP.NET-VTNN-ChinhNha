using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace ChinhNha.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ICartService _cartService;

    public AccountController(
        IAuthService authService,
        ICartService cartService)
    {
        _authService = authService;
        _cartService = cartService;
    }

    private string GetOrCreateSessionId()
    {
        var sessionId = HttpContext.Session.GetString("SessionId");
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("SessionId", sessionId);
        }
        return sessionId;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Nếu đã đăng nhập thì redirect về Home
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (ModelState.IsValid)
        {
            var loginId = NormalizeEmailOrPhone(model.Email);
            if (loginId == null)
            {
                ModelState.AddModelError(nameof(model.Email), "Vui lòng nhập đúng định dạng Email hoặc SĐT.");
                return View(model);
            }

            model.Email = loginId;

            var result = await _authService.LoginAsync(loginId, model.Password, model.RememberMe);
            if (result.Succeeded && !string.IsNullOrEmpty(result.UserId))
            {
                // ====================================================
                // Merge giỏ hàng Guest (Session) -> User (Database)
                // ====================================================
                var sessionId = HttpContext.Session.GetString("SessionId");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    try
                    {
                        await _cartService.MergeGuestCartToUserCartAsync(sessionId, result.UserId);
                    }
                    catch
                    {
                        // Lỗi merge cart không phải lỗi nghiêm trọng -> bỏ qua
                    }
                }

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                if (result.Roles.Contains("Admin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Email hoặc mật khẩu không chính xác.");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var loginId = NormalizeEmailOrPhone(model.Email);
            if (loginId == null)
            {
                ModelState.AddModelError(nameof(model.Email), "Vui lòng nhập đúng định dạng Email hoặc SĐT.");
                return View(model);
            }

            model.Email = loginId;

            var result = await _authService.RegisterAsync(model.FullName, loginId, model.Password, "Customer");

            if (result.Succeeded && !string.IsNullOrEmpty(result.UserId))
            {
                // Merge giỏ hàng nếu có
                var sessionId = HttpContext.Session.GetString("SessionId");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    try { await _cartService.MergeGuestCartToUserCartAsync(sessionId, result.UserId); }
                    catch { }
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng ký thất bại.");
        }

        return View(model);
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            await _authService.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        if (model.CurrentPassword == model.NewPassword)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu mới phải khác mật khẩu hiện tại.");
            return View(model);
        }

        var result = await _authService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đổi mật khẩu thất bại.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToAction("Profile", "Customer");
    }

    private static string? NormalizeEmailOrPhone(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();

        if (value.Contains('@'))
        {
            return Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")
                ? value.ToLowerInvariant()
                : null;
        }

        var digits = Regex.Replace(value, @"\D", string.Empty);

        if (digits.StartsWith("84") && digits.Length == 11)
        {
            digits = "0" + digits.Substring(2);
        }

        if (digits.Length == 10 && digits.StartsWith("0"))
        {
            return digits;
        }

        return null;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Xóa session giỏ hàng guest sau khi logout
        HttpContext.Session.Remove("SessionId");

        await _authService.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}
