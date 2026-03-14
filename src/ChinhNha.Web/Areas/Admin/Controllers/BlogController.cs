using ChinhNha.Application.DTOs.Requests;
using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class BlogController : Controller
{
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly IBlogService _blogService;
    private readonly IWebHostEnvironment _environment;

    public BlogController(IBlogService blogService, IWebHostEnvironment environment)
    {
        _blogService = blogService;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var posts = await _blogService.GetAllPostsAsync();
        return View(posts);
    }

    private async Task PopulateDropdowns(BlogFormViewModel model)
    {
        var cats = await _blogService.GetCategoriesAsync();
        model.Categories = cats.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new BlogFormViewModel { IsPublished = true };
        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogFormViewModel model)
    {
        if (model.ImageFile != null)
        {
            var uploadedImageUrl = await SaveImageAsync(model.ImageFile);
            if (uploadedImageUrl == null)
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Chỉ chấp nhận ảnh JPG, JPEG, PNG, WEBP, GIF.");
            }
            else
            {
                model.ImageUrl = uploadedImageUrl;
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        var req = new CreatePostRequest
        {
            Title = model.Title,
            Content = model.Content,
            Excerpt = model.Excerpt,
            ImageUrl = model.ImageUrl,
            CategoryId = model.CategoryId,
            IsPublished = model.IsPublished,
            MetaTitle = model.MetaTitle,
            MetaDescription = model.MetaDescription
        };

        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _blogService.CreatePostAsync(req, authorId);
        
        TempData["SuccessMessage"] = "Tạo bài viết thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _blogService.GetPostByIdAsync(id);
        if (post == null) return NotFound();

        var model = new BlogFormViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Excerpt = post.Summary,
            ImageUrl = post.FeaturedImageUrl,
            CategoryId = post.CategoryId,
            IsPublished = post.IsPublished,
            MetaTitle = post.MetaTitle,
            MetaDescription = post.MetaDescription
        };

        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BlogFormViewModel model)
    {
        if (model.ImageFile != null)
        {
            var uploadedImageUrl = await SaveImageAsync(model.ImageFile);
            if (uploadedImageUrl == null)
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Chỉ chấp nhận ảnh JPG, JPEG, PNG, WEBP, GIF.");
            }
            else
            {
                model.ImageUrl = uploadedImageUrl;
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        var req = new UpdatePostRequest
        {
            Id = model.Id,
            Title = model.Title,
            Content = model.Content,
            Excerpt = model.Excerpt,
            ImageUrl = model.ImageUrl,
            CategoryId = model.CategoryId,
            IsPublished = model.IsPublished,
            MetaTitle = model.MetaTitle,
            MetaDescription = model.MetaDescription
        };

        var success = await _blogService.UpdatePostAsync(req);
        if (!success) return NotFound();

        TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _blogService.DeletePostAsync(id);
        if (success) TempData["SuccessMessage"] = "Đã xoá bài viết.";
        else TempData["ErrorMessage"] = "Có lỗi xảy ra khi xoá.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string?> SaveImageAsync(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            return null;
        }

        var mediaFolder = Path.Combine(_environment.WebRootPath, "uploads", "media");
        Directory.CreateDirectory(mediaFolder);

        var safeName = Path.GetFileNameWithoutExtension(imageFile.FileName)
            .Replace(" ", "-")
            .Replace("..", string.Empty);
        var uniqueName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{safeName}{extension}";
        var filePath = Path.Combine(mediaFolder, uniqueName);

        await using var stream = System.IO.File.Create(filePath);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/media/{uniqueName}";
    }
}
