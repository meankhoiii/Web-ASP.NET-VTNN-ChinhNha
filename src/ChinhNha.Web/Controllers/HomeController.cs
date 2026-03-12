using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace ChinhNha.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;
    private readonly IRepository<ProductCategory> _categoryRepo;
    private readonly IBlogService _blogService;
    private readonly IRepository<ContactMessage> _contactMessageRepo;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public HomeController(
        ILogger<HomeController> logger, 
        IProductService productService,
        IRepository<ProductCategory> categoryRepo,
        IBlogService blogService,
        IRepository<ContactMessage> contactMessageRepo,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _logger = logger;
        _productService = productService;
        _categoryRepo = categoryRepo;
        _blogService = blogService;
        _contactMessageRepo = contactMessageRepo;
        _emailService = emailService;
        _configuration = configuration;
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(string name, string email, string phone, string message)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Vui lòng điền đầy đủ thông tin bắt buộc (Tên, Email, Tin nhắn)";
            return RedirectToAction(nameof(Contact));
        }

        var contactMessage = new ContactMessage
        {
            FullName = name.Trim(),
            Email = email.Trim(),
            Phone = phone?.Trim() ?? string.Empty,
            Subject = "Liên hệ từ website",
            Message = message.Trim()
        };

        await _contactMessageRepo.AddAsync(contactMessage);

        _logger.LogInformation("Contact form submitted: Name={Name}, Email={Email}, Phone={Phone}", name, email, phone);

        var adminEmail = _configuration["Email:AdminNotificationAddress"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            await _emailService.SendAsync(
                adminEmail,
                $"[ChinhNha] Liên hệ mới từ {name}",
                $"<h3>Liên hệ mới từ website</h3><p><strong>Họ tên:</strong> {name}</p><p><strong>Email:</strong> {email}</p><p><strong>Số điện thoại:</strong> {phone}</p><p><strong>Nội dung:</strong><br>{message}</p>",
                $"Liên hệ mới từ {name} | Email: {email} | SĐT: {phone} | Nội dung: {message}"
            );
        }

        await _emailService.SendAsync(
            email.Trim(),
            "ChinhNha đã nhận được liên hệ của bạn",
            $"<p>Xin chào <strong>{name}</strong>,</p><p>Chúng tôi đã nhận được thông tin liên hệ của bạn và sẽ phản hồi trong vòng 24 giờ làm việc.</p><p><strong>Nội dung bạn gửi:</strong><br>{message}</p><p>Trân trọng,<br>ChinhNha</p>",
            $"Xin chào {name}, ChinhNha đã nhận được liên hệ của bạn và sẽ phản hồi trong vòng 24 giờ làm việc. Nội dung: {message}"
        );
        
        TempData["Success"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi trong 24 giờ.";
        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
