using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Models.Account;
using Microsoft.AspNetCore.Mvc;

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
            var result = await _authService.LoginAsync(model.Email, model.Password, model.RememberMe);
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
            var result = await _authService.RegisterAsync(model.FullName, model.Email, model.Password, "Customer");

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
