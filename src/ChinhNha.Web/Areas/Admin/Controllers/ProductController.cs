using ChinhNha.Application.DTOs.Products;
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
[Authorize(Policy = "AdminOnly")]
public class ProductController : Controller
{
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly IProductService _productService;
    private readonly IRepository<ProductCategory> _categoryRepo;
    private readonly IRepository<Supplier> _supplierRepo;
    private readonly IRepository<ProductVariant> _variantRepo;
    private readonly IWebHostEnvironment _environment;

    public ProductController(
        IProductService productService,
        IRepository<ProductCategory> categoryRepo,
        IRepository<Supplier> supplierRepo,
        IRepository<ProductVariant> variantRepo,
        IWebHostEnvironment environment)
    {
        _productService = productService;
        _categoryRepo = categoryRepo;
        _supplierRepo = supplierRepo;
        _variantRepo = variantRepo;
        _environment = environment;
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
            ImportPrice = model.ImportPrice,
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
            ImportPrice = product.ImportPrice,
            BasePrice = product.BasePrice,
            SalePrice = product.SalePrice,
            StockQuantity = product.StockQuantity,
            MinStockLevel = product.MinStockLevel,
            Unit = product.Unit,
            Weight = product.Weight,
            IsFeatured = product.IsFeatured,
            IsActive = product.IsActive,
            ManufacturerUrl = product.ManufacturerUrl,
            ImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            Variants = product.Variants
                .OrderBy(v => v.DisplayOrder).ThenBy(v => v.Id)
                .Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    VariantName = v.VariantName,
                    SKU = v.SKU,
                    Price = v.Price,
                    SalePrice = v.SalePrice,
                    StockQuantity = v.StockQuantity,
                    Weight = v.Weight,
                    IsActive = v.IsActive,
                    DisplayOrder = v.DisplayOrder
                }).ToList()
        };

        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormViewModel model)
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
            ImportPrice = model.ImportPrice,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveVariant([FromForm] SaveVariantRequest model)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.VariantName))
            return BadRequest(new { error = "Dữ liệu không hợp lệ" });

        // Prevent negative sale price greater than price
        if (model.SalePrice.HasValue && model.SalePrice <= 0)
            model.SalePrice = null;
        if (model.Weight.HasValue && model.Weight <= 0)
            model.Weight = null;

        ProductVariant? variant = null;

        if (model.Id == 0)
        {
            variant = new ProductVariant
            {
                ProductId = model.ProductId,
                VariantName = model.VariantName.Trim(),
                SKU = string.IsNullOrWhiteSpace(model.SKU) ? null : model.SKU.Trim(),
                Price = model.Price,
                SalePrice = model.SalePrice,
                StockQuantity = model.StockQuantity,
                Weight = model.Weight,
                IsActive = model.IsActive,
                DisplayOrder = model.DisplayOrder
            };
            await _variantRepo.AddAsync(variant);
        }
        else
        {
            variant = await _variantRepo.GetByIdAsync(model.Id);
            if (variant == null || variant.ProductId != model.ProductId)
                return NotFound(new { error = "Không tìm thấy biến thể" });

            variant.VariantName = model.VariantName.Trim();
            variant.SKU = string.IsNullOrWhiteSpace(model.SKU) ? null : model.SKU.Trim();
            variant.Price = model.Price;
            variant.SalePrice = model.SalePrice;
            variant.StockQuantity = model.StockQuantity;
            variant.Weight = model.Weight;
            variant.IsActive = model.IsActive;
            variant.DisplayOrder = model.DisplayOrder;
            await _variantRepo.UpdateAsync(variant);
        }

        return Json(new
        {
            id = variant!.Id,
            productId = variant.ProductId,
            variantName = variant.VariantName,
            sku = variant.SKU,
            price = variant.Price,
            salePrice = variant.SalePrice,
            stockQuantity = variant.StockQuantity,
            weight = variant.Weight,
            isActive = variant.IsActive,
            displayOrder = variant.DisplayOrder
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(int id, int productId)
    {
        var variant = await _variantRepo.GetByIdAsync(id);
        if (variant == null || variant.ProductId != productId)
            return NotFound(new { error = "Không tìm thấy biến thể" });

        await _variantRepo.DeleteAsync(variant);
        return Json(new { success = true });
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
