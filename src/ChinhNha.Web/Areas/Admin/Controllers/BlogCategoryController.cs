using ChinhNha.Application.Helpers;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class BlogCategoryController : Controller
{
    private readonly IRepository<BlogCategory> _categoryRepository;

    public BlogCategoryController(IRepository<BlogCategory> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryRepository.ListAllAsync();
        var ordered = categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
        return View(ordered);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BlogCategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var category = new BlogCategory
        {
            Name = model.Name.Trim(),
            Slug = SlugHelper.GenerateSlug(model.Name),
            Description = model.Description,
            DisplayOrder = model.DisplayOrder
        };

        await _categoryRepository.AddAsync(category);
        TempData["SuccessMessage"] = "Đã tạo danh mục bài viết.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        var model = new BlogCategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BlogCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var category = await _categoryRepository.GetByIdAsync(model.Id);
        if (category == null)
        {
            return NotFound();
        }

        category.Name = model.Name.Trim();
        category.Slug = SlugHelper.GenerateSlug(model.Name);
        category.Description = model.Description;
        category.DisplayOrder = model.DisplayOrder;

        await _categoryRepository.UpdateAsync(category);
        TempData["SuccessMessage"] = "Đã cập nhật danh mục bài viết.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy danh mục cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _categoryRepository.DeleteAsync(category);
            TempData["SuccessMessage"] = "Đã xóa danh mục bài viết.";
        }
        catch
        {
            TempData["ErrorMessage"] = "Không thể xóa danh mục do đang có dữ liệu liên quan.";
        }

        return RedirectToAction(nameof(Index));
    }
}
