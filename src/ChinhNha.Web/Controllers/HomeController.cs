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

    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Contact(string name, string email, string phone, string message)
    {
        // TODO: In a real application, save contact form to database or send email
        // For now, just return success message
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Vui lòng điền đầy đủ thông tin bắt buộc (Tên, Email, Tin nhắn)";
            return RedirectToAction(nameof(Contact));
        }

        // Log the contact form submission
        _logger.LogInformation($"Contact form submitted: Name={name}, Email={email}, Phone={phone}, Message={message}");
        
        TempData["Success"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi trong 24 giờ.";
        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
