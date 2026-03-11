using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ChinhNha.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;

    public HomeController(ILogger<HomeController> logger, IProductService productService)
    {
        _logger = logger;
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        // Use GetFeaturedProductsAsync (or fallback to top 8 from GetAll if empty)
        var featured = await _productService.GetFeaturedProductsAsync();
        
        var productsList = featured.ToList();
        if (!productsList.Any())
        {
            var all = await _productService.GetAllProductsAsync();
            productsList = all.Take(8).ToList();
        }

        var model = new HomeViewModel
        {
            FeaturedProducts = productsList
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
