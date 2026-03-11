using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
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

    public async Task<IActionResult> Index()
    {
        string? userId = null; // TODO: Lấy UserID thực tế khi tích hợp Auth
        var sessionId = GetOrCreateSessionId();
        
        var cart = await _cartService.GetCartAsync(userId, sessionId);
        
        var model = new CartViewModel
        {
            Cart = cart
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        try
        {
            string? userId = null; // TODO: Auth
            var sessionId = GetOrCreateSessionId();

            await _cartService.AddItemToCartAsync(userId, sessionId, productId, quantity);
            
            // Lấy lại giỏ hàng để cập nhật số lượng
            var cart = await _cartService.GetCartAsync(userId, sessionId);
            var totalItems = cart.Items.Sum(c => c.Quantity);

            return Json(new { success = true, totalItems = totalItems, message = "Thêm vào giỏ thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int cartItemId)
    {
        await _cartService.RemoveItemFromCartAsync(cartItemId);

        return RedirectToAction(nameof(Index));
    }
}
