using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IProductReviewService _productReviewService;
    private readonly IRepository<ProductCategory> _productCategoryRepo;
    private const int PageSize = 12;

    public ProductController(
        IProductService productService,
        IProductReviewService productReviewService,
        IRepository<ProductCategory> productCategoryRepo)
    {
        _productService = productService;
        _productReviewService = productReviewService;
        _productCategoryRepo = productCategoryRepo;
    }

    public async Task<IActionResult> Index(int? categoryId, string? searchQuery, int pageNumber = 1)
    {
        ViewData["MetaDescription"] = "Cua hang phan bon Chinh Nha voi bo loc tim kiem, danh muc, gia va thong tin san pham nong nghiep chi tiet.";

        var categories = (await _productCategoryRepo.ListAllAsync())
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToList();

        IEnumerable<ProductDto> products;
        string? selectedCategorySlug = null;
        
        if (categoryId.HasValue)
        {
            products = await _productService.GetProductsByCategoryAsync(categoryId.Value);
            selectedCategorySlug = categories.FirstOrDefault(category => category.Id == categoryId.Value)?.Slug;
        }
        else
        {
            products = await _productService.GetAllProductsAsync();
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            products = products.Where(p => p.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) 
                                        || (p.Description != null && p.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));
        }

        var productsList = products.ToList();
        int totalCount = productsList.Count;
        
        // Basic in-memory pagination
        var pagedItems = productsList.Skip((pageNumber - 1) * PageSize).Take(PageSize).ToList();

        var model = new ProductListViewModel
        {
            Products = pagedItems,
            Categories = categories,
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize),
            SearchQuery = searchQuery,
            CategorySlug = selectedCategorySlug
        };

        return View(model);
    }

    public async Task<IActionResult> Details(string slug)
    {
        var product = await _productService.GetProductBySlugAsync(slug);
        
        if (product == null)
            return NotFound();

        // Get some related products (same category)
        var relatedResult = await _productService.GetProductsByCategoryAsync(product.CategoryId);

        var reviewStats = await _productReviewService.GetProductReviewStatsAsync(product.Id);
        var reviews = await _productReviewService.GetProductReviewsAsync(product.Id, 1, 5, true);

        var model = new ProductDetailsViewModel
        {
            Product = product,
            RelatedProducts = relatedResult.Where(p => p.Id != product.Id).Take(4),
            Reviews = reviews,
            ReviewStats = reviewStats
        };

        ViewData["MetaDescription"] = product.MetaDescription ?? product.ShortDescription ?? product.Description ?? product.Name;
        ViewData["OgImage"] = product.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl;

        return View(model);
    }

    public async Task<IActionResult> ByCategory(string slug, string? searchQuery, int pageNumber = 1)
    {
        var products = await _productService.GetProductsByCategorySlugAsync(slug);
        var categories = (await _productCategoryRepo.ListAllAsync())
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToList();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            products = products.Where(p => p.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) 
                                        || (p.Description != null && p.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));
        }

        var productsList = products.ToList();
        int totalCount = productsList.Count;
        
        // Basic in-memory pagination
        var pagedItems = productsList.Skip((pageNumber - 1) * PageSize).Take(PageSize).ToList();

        var model = new ProductListViewModel
        {
            Products = pagedItems,
            Categories = categories,
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize),
            SearchQuery = searchQuery,
            CategorySlug = slug
        };

        return View("Index", model); // Use same Index view
    }
}
