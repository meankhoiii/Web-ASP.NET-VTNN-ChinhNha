using ChinhNha.Application.Interfaces;
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
            RecentOrders = recentOrders.ToList()
        };

        return View(model);
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

    public IActionResult Wishlist()
    {
        return View();
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
