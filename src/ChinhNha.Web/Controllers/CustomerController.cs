using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Enums;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChinhNha.Web.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(ICustomerService customerService, ILogger<CustomerController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var profile = await _customerService.GetCustomerProfileAsync(userId);
        if (profile == null)
            return NotFound();

        var recentOrders = await _customerService.GetCustomerOrdersAsync(userId, pageNumber: 1, pageSize: 5);

        var model = new CustomerDashboardViewModel
        {
            Profile = profile,
            RecentOrders = recentOrders.ToList(),
            Notifications = BuildOrderNotifications(recentOrders)
        };

        return View(model);
    }

    private static List<CustomerNotificationItemViewModel> BuildOrderNotifications(IEnumerable<ChinhNha.Application.DTOs.Orders.OrderDto> orders)
    {
        var notifications = new List<CustomerNotificationItemViewModel>();

        foreach (var order in orders.OrderByDescending(o => o.OrderDate).Take(8))
        {
            if (order.Status == OrderStatus.Pending)
                continue;

            var (title, message, type) = order.Status switch
            {
                OrderStatus.Confirmed => (
                    "Don hang da duoc xac nhan",
                    $"Don #{order.Id} da duoc xac nhan va dang cho xu ly.",
                    "info"),
                OrderStatus.Processing => (
                    "Don hang dang duoc xu ly",
                    $"Don #{order.Id} dang trong qua trinh dong goi.",
                    "info"),
                OrderStatus.Shipping => (
                    "Don hang dang giao",
                    $"Don #{order.Id} dang tren duong giao den ban.",
                    "warning"),
                OrderStatus.Delivered => (
                    "Don hang da giao thanh cong",
                    $"Don #{order.Id} da duoc giao. Cam on ban da mua hang!",
                    "success"),
                OrderStatus.Cancelled => (
                    "Don hang da bi huy",
                    $"Don #{order.Id} da bi huy. Neu can ho tro, vui long lien he cua hang.",
                    "danger"),
                _ => (
                    "Cap nhat don hang",
                    $"Don #{order.Id} da chuyen sang trang thai {order.Status}.",
                    "info")
            };

            notifications.Add(new CustomerNotificationItemViewModel
            {
                Title = title,
                Message = message,
                Type = type
            });
        }

        return notifications;
    }

    public async Task<IActionResult> Orders(int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        const int pageSize = 10;
        var orders = await _customerService.GetCustomerOrdersAsync(userId, page, pageSize);
        var totalOrders = await _customerService.GetTotalOrdersCountAsync(userId);
        var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

        var model = new CustomerOrdersViewModel
        {
            Orders = orders.ToList(),
            CurrentPage = page,
            TotalPages = totalPages,
            TotalOrders = totalOrders
        };

        return View(model);
    }

    public async Task<IActionResult> OrderDetail(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var order = await _customerService.GetCustomerOrderDetailAsync(userId, id);
        if (order == null)
            return NotFound();

        return View(order);
    }

    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var profile = await _customerService.GetCustomerProfileAsync(userId);
        if (profile == null)
            return NotFound();

        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string phone)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ModelState.AddModelError(nameof(fullName), "Họ tên không được để trống.");
            var profile = await _customerService.GetCustomerProfileAsync(userId);
            return View("Profile", profile);
        }

        try
        {
            var success = await _customerService.UpdateCustomerProfileAsync(userId, fullName, phone);
            if (success)
            {
                TempData["SuccessMessage"] = "Thông tin được cập nhật thành công!";
                return RedirectToAction(nameof(Profile));
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
            return RedirectToAction(nameof(Profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer profile for user {UserId}", userId);
            TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
