using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IActionResult> Index(OrderStatus? status = null)
    {
        var orders = await _orderService.GetAllOrdersAsync(status);
        ViewBag.CurrentStatus = status;
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
    {
        var success = await _orderService.UpdateOrderStatusAsync(id, newStatus);
        
        if (success)
            TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn hàng #{id} thành công!";
        else
            TempData["ErrorMessage"] = "Cập nhật thất bại. Vui lòng kiểm tra lại.";

        return RedirectToAction(nameof(Details), new { id });
    }
}
