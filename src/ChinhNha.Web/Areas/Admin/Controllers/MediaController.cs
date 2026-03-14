using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class MediaController : Controller
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly IWebHostEnvironment _environment;

    public MediaController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(BuildViewModel(isPicker: false, fieldId: null));
    }

    [HttpGet]
    public IActionResult Picker(string? fieldId)
    {
        return View("Index", BuildViewModel(isPicker: true, fieldId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file, bool isPicker = false, string? fieldId = null)
    {
        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn tệp ảnh để tải lên.";
            return RedirectToAction(isPicker ? nameof(Picker) : nameof(Index), new { fieldId });
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            TempData["ErrorMessage"] = "Chỉ chấp nhận ảnh JPG, JPEG, PNG, WEBP, GIF.";
            return RedirectToAction(isPicker ? nameof(Picker) : nameof(Index), new { fieldId });
        }

        var uploadFolder = GetMediaFolderPath();
        Directory.CreateDirectory(uploadFolder);

        var safeName = Path.GetFileNameWithoutExtension(file.FileName)
            .Replace(" ", "-")
            .Replace("..", string.Empty);
        var uniqueName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{safeName}{extension}";
        var filePath = Path.Combine(uploadFolder, uniqueName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        TempData["SuccessMessage"] = "Tải ảnh lên thành công.";
        return RedirectToAction(isPicker ? nameof(Picker) : nameof(Index), new { fieldId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string fileName, bool isPicker = false, string? fieldId = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            TempData["ErrorMessage"] = "Tên tệp không hợp lệ.";
            return RedirectToAction(isPicker ? nameof(Picker) : nameof(Index), new { fieldId });
        }

        var cleanName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(GetMediaFolderPath(), cleanName);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
            TempData["SuccessMessage"] = "Đã xóa tệp ảnh.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy tệp cần xóa.";
        }

        return RedirectToAction(isPicker ? nameof(Picker) : nameof(Index), new { fieldId });
    }

    private MediaLibraryViewModel BuildViewModel(bool isPicker, string? fieldId)
    {
        var folder = GetMediaFolderPath();
        Directory.CreateDirectory(folder);

        var files = Directory
            .GetFiles(folder)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Select(file => new MediaFileItemViewModel
            {
                FileName = file.Name,
                Url = $"/uploads/media/{file.Name}",
                SizeBytes = file.Length,
                CreatedAt = file.CreationTimeUtc
            })
            .ToList();

        return new MediaLibraryViewModel
        {
            IsPicker = isPicker,
            FieldId = fieldId,
            Files = files
        };
    }

    private string GetMediaFolderPath()
    {
        return Path.Combine(_environment.WebRootPath, "uploads", "media");
    }
}
