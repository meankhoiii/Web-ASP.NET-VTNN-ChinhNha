using ChinhNha.Application.DTOs.Requests;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IRepository<ProductCategory> _categoryRepo;
    private readonly IRepository<Supplier> _supplierRepo;

    public ProductController(
        IProductService productService, 
        IRepository<ProductCategory> categoryRepo, 
        IRepository<Supplier> supplierRepo)
    {
        _productService = productService;
        _categoryRepo = categoryRepo;
        _supplierRepo = supplierRepo;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllProductsAsync();
        return View(products);
    }

    private async Task PopulateDropdowns(ProductFormViewModel model)
    {
        var categories = await _categoryRepo.ListAllAsync();
        var suppliers = await _supplierRepo.ListAllAsync();

        model.Categories = categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
        model.Suppliers = suppliers.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ProductFormViewModel();
        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        var req = new CreateProductRequest
        {
            Name = model.Name,
            SKU = model.SKU,
            ShortDescription = model.ShortDescription,
            Description = model.Description,
            UsageInstructions = model.UsageInstructions,
            TechnicalInfo = model.TechnicalInfo,
            CategoryId = model.CategoryId,
            SupplierId = model.SupplierId,
            BasePrice = model.BasePrice,
            SalePrice = model.SalePrice,
            StockQuantity = model.StockQuantity,
            MinStockLevel = model.MinStockLevel,
            Unit = model.Unit,
            Weight = model.Weight,
            IsFeatured = model.IsFeatured,
            IsActive = model.IsActive,
            ManufacturerUrl = model.ManufacturerUrl,
            InitialImageUrl = model.ImageUrl
        };

        var id = await _productService.CreateProductAsync(req);
        TempData["SuccessMessage"] = "Sản phẩm đã được tạo thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            UsageInstructions = product.UsageInstructions,
            TechnicalInfo = product.TechnicalInfo,
            CategoryId = product.CategoryId,
            SupplierId = product.SupplierId,
            BasePrice = product.BasePrice,
            SalePrice = product.SalePrice,
            StockQuantity = product.StockQuantity,
            MinStockLevel = product.MinStockLevel,
            Unit = product.Unit,
            Weight = product.Weight,
            IsFeatured = product.IsFeatured,
            IsActive = product.IsActive,
            ManufacturerUrl = product.ManufacturerUrl,
            ImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
        };

        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        var req = new UpdateProductRequest
        {
            Id = model.Id,
            Name = model.Name,
            SKU = model.SKU,
            ShortDescription = model.ShortDescription,
            Description = model.Description,
            UsageInstructions = model.UsageInstructions,
            TechnicalInfo = model.TechnicalInfo,
            CategoryId = model.CategoryId,
            SupplierId = model.SupplierId,
            BasePrice = model.BasePrice,
            SalePrice = model.SalePrice,
            StockQuantity = model.StockQuantity,
            MinStockLevel = model.MinStockLevel,
            Unit = model.Unit,
            Weight = model.Weight,
            IsFeatured = model.IsFeatured,
            IsActive = model.IsActive,
            ManufacturerUrl = model.ManufacturerUrl,
            NewImageUrl = model.ImageUrl
        };

        var success = await _productService.UpdateProductAsync(req);
        if (!success) return NotFound();

        TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _productService.DeleteProductAsync(id);
        if (success)
        {
            TempData["SuccessMessage"] = "Sản phẩm đã bị xoá!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm hoặc có lỗi xảy ra.";
        }
        return RedirectToAction(nameof(Index));
    }
}
