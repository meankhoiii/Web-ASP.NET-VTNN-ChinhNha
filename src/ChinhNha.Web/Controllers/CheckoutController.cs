using ChinhNha.Application.DTOs.Orders;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Enums;
using ChinhNha.Web.Hubs;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChinhNha.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IVNPayService _vnpayService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ICartService cartService,
        IOrderService orderService,
        IVNPayService vnpayService,
        IEmailService emailService,
        IConfiguration configuration,
        IHubContext<NotificationHub> notificationHub,
        ILogger<CheckoutController> logger)
    {
        _cartService = cartService;
        _orderService = orderService;
        _vnpayService = vnpayService;
        _emailService = emailService;
        _configuration = configuration;
        _notificationHub = notificationHub;
        _logger = logger;
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
                shippingNote: model.Note,
                paymentMethod: model.PaymentMethod
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

            await SendOrderCreatedNotificationsAsync(order, model);

            return RedirectToAction("Success", new { id = order.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout failed for user {UserId}", userId);
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
            var paymentStatus = response.Success ? PaymentStatus.Paid : PaymentStatus.Failed;
            await _orderService.UpdatePaymentResultAsync(
                orderId,
                PaymentMethod.VNPay,
                paymentStatus,
                response.TransactionId,
                response.Success
                    ? $"VNPay thanh toán thành công. Mã phản hồi: {response.VnPayResponseCode}"
                    : $"VNPay thanh toán thất bại. Mã phản hồi: {response.VnPayResponseCode}"
            );

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();

            if (response.Success)
            {
                await SendPaymentNotificationsAsync(order, response, true);
                TempData["PaymentMessage"] = $"Thanh toán VNPay thành công! (Mã giao dịch: {response.TransactionId})";
            }
            else
            {
                await SendPaymentNotificationsAsync(order, response, false);
                TempData["PaymentMessage"] = $"Thanh toán VNPay thất bại hoặc bị hủy.";
            }

            return RedirectToAction("Success", new { id = orderId });
        }

        return RedirectToAction("Index", "Home");
    }

    private async Task SendOrderCreatedNotificationsAsync(OrderDto order, CheckoutViewModel model)
    {
        // Real-time notification to admin via SignalR
        await _notificationHub.Clients.Group("AdminGroup").SendAsync("NewOrderNotification", new
        {
            orderId = order.Id,
            customerName = order.UserFullName ?? model.ReceiverName ?? "Khách",
            totalAmount = order.TotalAmount,
            message = $"Đơn hàng mới #{order.Id} — {order.UserFullName ?? model.ReceiverName} — {order.TotalAmount:N0}đ"
        });

        if (!string.IsNullOrWhiteSpace(order.UserEmail))
        {
            await _emailService.SendAsync(
                order.UserEmail,
                $"[ChinhNha] Xác nhận đơn hàng #{order.Id}",
                $"<p>Xin chào <strong>{model.ReceiverName}</strong>,</p><p>Đơn hàng <strong>#{order.Id}</strong> đã được tạo thành công.</p><p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p><p><strong>Tổng tiền:</strong> {order.TotalAmount:N0}đ</p><p>Chúng tôi sẽ sớm liên hệ để xác nhận đơn hàng.</p>",
                $"Don hang #{order.Id} da duoc tao. Phuong thuc thanh toan: {order.PaymentMethod}. Tong tien: {order.TotalAmount:N0}đ."
            );
        }

        var adminEmail = _configuration["Email:AdminNotificationAddress"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            await _emailService.SendAsync(
                adminEmail,
                $"[ChinhNha] Đơn hàng mới #{order.Id}",
                $"<p>Có đơn hàng mới <strong>#{order.Id}</strong>.</p><p><strong>Khách hàng:</strong> {order.UserFullName}</p><p><strong>Người nhận:</strong> {model.ReceiverName}</p><p><strong>Số điện thoại:</strong> {model.ReceiverPhone}</p><p><strong>Thanh toán:</strong> {order.PaymentMethod}</p><p><strong>Tổng tiền:</strong> {order.TotalAmount:N0}đ</p><p><strong>Địa chỉ:</strong> {model.ShippingAddress}</p>",
                $"Don hang moi #{order.Id}. Khach hang: {order.UserFullName}. Nguoi nhan: {model.ReceiverName}. So dien thoai: {model.ReceiverPhone}. Thanh toan: {order.PaymentMethod}. Tong tien: {order.TotalAmount:N0}đ."
            );
        }
    }

    private async Task SendPaymentNotificationsAsync(OrderDto order, PaymentResponseDto response, bool isSuccess)
    {
        if (!string.IsNullOrWhiteSpace(order.UserEmail))
        {
            await _emailService.SendAsync(
                order.UserEmail,
                isSuccess
                    ? $"[ChinhNha] Thanh toán VNPay thành công cho đơn #{order.Id}"
                    : $"[ChinhNha] Thanh toán VNPay chưa hoàn tất cho đơn #{order.Id}",
                isSuccess
                    ? $"<p>Thanh toán cho đơn hàng <strong>#{order.Id}</strong> đã thành công.</p><p><strong>Mã giao dịch:</strong> {response.TransactionId}</p><p><strong>Tổng tiền:</strong> {order.TotalAmount:N0}đ</p>"
                    : $"<p>Thanh toán VNPay cho đơn hàng <strong>#{order.Id}</strong> chưa hoàn tất.</p><p><strong>Mã phản hồi:</strong> {response.VnPayResponseCode}</p><p>Bạn có thể thử lại hoặc liên hệ ChinhNha để được hỗ trợ.</p>",
                isSuccess
                    ? $"Thanh toan VNPay thanh cong cho don hang #{order.Id}. Ma giao dich: {response.TransactionId}."
                    : $"Thanh toan VNPay cho don hang #{order.Id} chua hoan tat. Ma phan hoi: {response.VnPayResponseCode}."
            );
        }

        var adminEmail = _configuration["Email:AdminNotificationAddress"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            await _emailService.SendAsync(
                adminEmail,
                isSuccess
                    ? $"[ChinhNha] VNPay thanh toán thành công cho đơn #{order.Id}"
                    : $"[ChinhNha] VNPay thanh toán thất bại cho đơn #{order.Id}",
                isSuccess
                    ? $"<p>Đơn hàng <strong>#{order.Id}</strong> đã thanh toán VNPay thành công.</p><p><strong>Mã giao dịch:</strong> {response.TransactionId}</p><p><strong>Tổng tiền:</strong> {order.TotalAmount:N0}đ</p>"
                    : $"<p>Đơn hàng <strong>#{order.Id}</strong> thanh toán VNPay chưa hoàn tất.</p><p><strong>Mã phản hồi:</strong> {response.VnPayResponseCode}</p><p><strong>Tổng tiền:</strong> {order.TotalAmount:N0}đ</p>",
                isSuccess
                    ? $"Don hang #{order.Id} da thanh toan VNPay thanh cong. Ma giao dich: {response.TransactionId}."
                    : $"Don hang #{order.Id} thanh toan VNPay chua hoan tat. Ma phan hoi: {response.VnPayResponseCode}."
            );
        }
    }
}
