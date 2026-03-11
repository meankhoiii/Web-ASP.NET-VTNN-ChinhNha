using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
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
            // Get forecasts for the selected product
            model.Forecasts = await _forecastService.GetForecastForProductAsync(productId.Value, 4); 
            // Gets next 4 periods (weeks/months depending on training data)
        }

        return View(model);
    }
}
