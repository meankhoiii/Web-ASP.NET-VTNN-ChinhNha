using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class InventoryController : Controller
{
    private readonly IProductService _productService;
    private readonly IInventoryForecastService _forecastService;

    public InventoryController(IProductService productService, IInventoryForecastService forecastService)
    {
        _productService = productService;
        _forecastService = forecastService;
    }

    public async Task<IActionResult> Index(int? productId)
    {
        var allProducts = await _productService.GetAllProductsAsync();
        
        var model = new InventoryForecastViewModel
        {
            Products = allProducts,
            SelectedProductId = productId
        };

        if (productId.HasValue)
        {
            model.Forecasts = await _forecastService.GetForecastForProductAsync(productId.Value, 4); 
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> TrainAi()
    {
        try
        {
            await _forecastService.TrainModelsAsync();
            TempData["SuccessMessage"] = "Đã cập nhật (huấn luyện) lại mô hình AI dự báo thành công với dữ liệu mới nhất.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Lỗi khi cập nhật mô hình: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
