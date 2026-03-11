using ChinhNha.Application.DTOs.Orders;
using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Controllers;

public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public CheckoutController(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
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
        string? userId = null; // TODO: Auth
        var sessionId = GetOrCreateSessionId();
        
        var cart = await _cartService.GetCartAsync(userId, sessionId);

        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart"); // No items to checkout
        }

        var model = new CheckoutViewModel
        {
            Cart = cart
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel model)
    {
        string? userId = null; // TODO: Auth
        var sessionId = GetOrCreateSessionId();
        
        var cart = await _cartService.GetCartAsync(userId, sessionId);
        model.Cart = cart; // Preserve cart data in case of validation error

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        try
        {
            var order = await _orderService.CreateOrderFromCartAsync(
                cartId: cart.Id,
                userId: userId ?? string.Empty,
                shippingName: model.ReceiverName,
                shippingPhone: model.ReceiverPhone,
                shippingAddress: model.ShippingAddress,
                shippingNote: model.Note
            );

            if (model.PaymentMethod == "VNPay")
            {
                // TODO: Integration phase 6 - Redirect to VNPay
                return RedirectToAction("Success", new { id = order.Id });
            }

            return RedirectToAction("Success", new { id = order.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đặt hàng: " + ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Success(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();

        return View(order);
    }
}
