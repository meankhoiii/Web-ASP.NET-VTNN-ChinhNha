using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private const int PageSize = 12;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index(int? categoryId, string? searchQuery, int pageNumber = 1)
    {
        IEnumerable<ProductDto> products;
        
        if (categoryId.HasValue)
        {
            products = await _productService.GetProductsByCategoryAsync(categoryId.Value);
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
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize),
            SearchQuery = searchQuery,
            CategorySlug = categoryId?.ToString() // Since model wants a slug, for now pass ID as a string or change model
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

        var model = new ProductDetailsViewModel
        {
            Product = product,
            RelatedProducts = relatedResult.Where(p => p.Id != product.Id).Take(4)
        };

        return View(model);
    }

    public async Task<IActionResult> ByCategory(string slug, string? searchQuery, int pageNumber = 1)
    {
        var products = await _productService.GetProductsByCategorySlugAsync(slug);

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
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize),
            SearchQuery = searchQuery,
            CategorySlug = slug
        };

        return View("Index", model); // Use same Index view
    }
}
