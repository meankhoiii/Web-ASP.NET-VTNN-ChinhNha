using ChinhNha.Application.Helpers;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ProductCategoryController : Controller
{
    private readonly IRepository<ProductCategory> _categoryRepository;

    public ProductCategoryController(IRepository<ProductCategory> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryRepository.ListAllAsync();
        var ordered = categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
        return View(ordered);
    }

    private async Task PopulateParentCategories(ProductCategoryFormViewModel model, int? excludeId = null)
    {
        var categories = await _categoryRepository.ListAllAsync();
        model.ParentCategories = categories
            .Where(c => !excludeId.HasValue || c.Id != excludeId.Value)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ProductCategoryFormViewModel { IsActive = true };
        await PopulateParentCategories(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateParentCategories(model);
            return View(model);
        }

        var category = new ProductCategory
        {
            Name = model.Name.Trim(),
            Slug = SlugHelper.GenerateSlug(model.Name),
            Description = model.Description,
            ImageUrl = model.ImageUrl,
            ParentCategoryId = model.ParentCategoryId,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _categoryRepository.AddAsync(category);
        TempData["SuccessMessage"] = "Đã tạo danh mục sản phẩm.";
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

        var model = new ProductCategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        };

        await PopulateParentCategories(model, category.Id);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateParentCategories(model, model.Id);
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
        category.ImageUrl = model.ImageUrl;
        category.ParentCategoryId = model.ParentCategoryId;
        category.DisplayOrder = model.DisplayOrder;
        category.IsActive = model.IsActive;

        await _categoryRepository.UpdateAsync(category);
        TempData["SuccessMessage"] = "Đã cập nhật danh mục sản phẩm.";
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
            TempData["SuccessMessage"] = "Đã xóa danh mục sản phẩm.";
        }
        catch
        {
            TempData["ErrorMessage"] = "Không thể xóa danh mục do đang có dữ liệu liên quan.";
        }

        return RedirectToAction(nameof(Index));
    }
}
