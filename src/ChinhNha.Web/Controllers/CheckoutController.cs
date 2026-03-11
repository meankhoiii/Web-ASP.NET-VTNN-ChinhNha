using ChinhNha.Application.DTOs.Orders;
using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChinhNha.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IVNPayService _vnpayService;

    public CheckoutController(ICartService cartService, IOrderService orderService, IVNPayService vnpayService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _vnpayService = vnpayService;
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
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Lấy UserID thực tể từ Claims
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
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                var paymentInfo = new PaymentInformationDto
                {
                    OrderType = "other",
                    Amount = order.TotalAmount,
                    OrderDescription = $"Thanh toán đơn hàng CHINHNHA{order.Id}",
                    Name = model.ReceiverName ?? "Guest",
                    OrderId = order.Id
                };

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var paymentUrl = _vnpayService.CreatePaymentUrl(paymentInfo, ipAddress);
                
                return Redirect(paymentUrl);
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

    [HttpGet]
    public async Task<IActionResult> PaymentCallback()
    {
        var queryDictionary = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var response = _vnpayService.PaymentExecute(queryDictionary);

        if (response == null || string.IsNullOrEmpty(response.OrderId))
        {
            return RedirectToAction("Index", "Home");
        }

        if (int.TryParse(response.OrderId, out int orderId))
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();

            if (response.Success)
            {
                // TODO: Gọi logic cập nhật trạng thái đơn hàng (Đã thanh toán) ở Application layer
                TempData["PaymentMessage"] = $"Thanh toán VNPay thành công! (Mã giao dịch: {response.TransactionId})";
            }
            else
            {
                TempData["PaymentMessage"] = $"Thanh toán VNPay thất bại hoặc bị hủy.";
            }

            return RedirectToAction("Success", new { id = orderId });
        }

        return RedirectToAction("Index", "Home");
    }
}
