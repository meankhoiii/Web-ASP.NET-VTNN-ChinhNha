using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

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
        ViewData["MetaDescription"] = "Chinh Nha cung cap phan bon va vat tu nong nghiep chinh hang, tu van mua vu, dat hang nhanh va giao tan noi.";

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubscribeNewsletter(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            TempData["NewsletterError"] = "Email khong hop le.";
            return RedirectToAction(nameof(Index));
        }

        var adminEmail = _configuration["Email:AdminNotificationAddress"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            await _emailService.SendAsync(
                adminEmail,
                "[ChinhNha] Dang ky newsletter moi",
                $"<p>Co nguoi dang ky nhan ban tin moi.</p><p><strong>Email:</strong> {email.Trim()}</p>");
        }

        await _emailService.SendAsync(
            email.Trim(),
            "Dang ky ban tin ChinhNha thanh cong",
            "<p>Cam on ban da dang ky nhan ban tin tu ChinhNha. Chung toi se gui thong tin khuyen mai va kinh nghiem canh tac phu hop.</p>");

        TempData["NewsletterSuccess"] = "Dang ky nhan ban tin thanh cong.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var products = await _productService.GetAllProductsAsync();
        var blogs = await _blogService.GetPublishedPostsAsync();

        var urlset = new XElement("urlset",
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
            BuildUrlElement(baseUrl + "/"),
            BuildUrlElement(baseUrl + "/Product"),
            BuildUrlElement(baseUrl + "/tin-tuc"),
            BuildUrlElement(baseUrl + "/Home/Contact"),
            products.Select(p => BuildUrlElement(baseUrl + "/san-pham/" + p.Slug)),
            blogs.Select(b => BuildUrlElement(baseUrl + "/tin-tuc/" + b.Slug))
        );

        var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
        return Content(document.ToString(), "application/xml");
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CallbackRequest([FromForm] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { message = "Vui lòng nhập số điện thoại." });

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 9 || digits.Length > 11)
            return BadRequest(new { message = "Số điện thoại không hợp lệ." });

        var contact = new ContactMessage
        {
            FullName = "Khách vãng lai",
            Phone = phone.Trim(),
            Subject = "Yêu cầu gọi lại",
            Message = $"Khách yêu cầu gọi lại qua số: {phone.Trim()}"
        };
        await _contactMessageRepo.AddAsync(contact);

        var adminEmail = _configuration["Email:AdminNotificationAddress"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            await _emailService.SendAsync(
                adminEmail,
                "[VTNN Chính Nha] Có khách yêu cầu gọi lại",
                $"<h3>Yêu cầu gọi lại</h3><p><strong>Số điện thoại:</strong> {phone.Trim()}</p><p>Vui lòng liên hệ lại với khách sớm nhất!</p>",
                $"Khách yêu cầu gọi lại: {phone.Trim()}"
            );
        }

        return Ok(new { message = "Đã nhận! Chúng tôi sẽ gọi lại ngay cho bà con." });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static XElement BuildUrlElement(string url)
    {
        return new XElement("url",
            new XElement("loc", url),
            new XElement("changefreq", "daily"),
            new XElement("priority", "0.8"));
    }
}
