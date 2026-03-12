using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ChinhNha.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;
    private readonly IRepository<ProductCategory> _categoryRepo;
    private readonly IBlogService _blogService;

    public HomeController(
        ILogger<HomeController> logger, 
        IProductService productService,
        IRepository<ProductCategory> categoryRepo,
        IBlogService blogService)
    {
        _logger = logger;
        _productService = productService;
        _categoryRepo = categoryRepo;
        _blogService = blogService;
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

        // Fetch Categories
        var categories = await _categoryRepo.ListAllAsync();
        
        // Fetch Latest Blogs (Take 3)
        var blogs = await _blogService.GetPublishedPostsAsync();
        var latestBlogs = blogs.OrderByDescending(b => b.PublishedAt).Take(3).ToList();

        var model = new HomeViewModel
        {
            FeaturedProducts = productsList,
            Categories = categories,
            LatestBlogs = latestBlogs
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
