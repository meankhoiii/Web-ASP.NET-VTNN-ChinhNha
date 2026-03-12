using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChinhNha.Infrastructure.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Product>> GetFeaturedProductsAsync()
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Images)
            .Where(p => p.IsFeatured && p.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Images)
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategorySlugAsync(string categorySlug)
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Images)
            .Where(p => p.Category != null && p.Category.Slug == categorySlug && p.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsWithDetailsAsync()
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Images)
            .ToListAsync();
    }

    public async Task<Product?> GetProductWithDetailsByIdAsync(int id)
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetProductWithDetailsBySlugAsync(string slug)
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(
        string? searchQuery = null,
        int? categoryId = null,
        int? supplierId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStockOnly = null,
        bool? featuredOnly = null,
        bool? onSaleOnly = null)
    {
        var query = _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Images)
            .AsQueryable();

        // Text search
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var lowerQuery = searchQuery.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(lowerQuery) ||
                (p.SKU != null && p.SKU.ToLower().Contains(lowerQuery)) ||
                (p.Description != null && p.Description.ToLower().Contains(lowerQuery)) ||
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(lowerQuery)));
        }

        // Category filter
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // Supplier filter
        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        // Price range filter
        if (minPrice.HasValue)
        {
            query = query.Where(p => (p.SalePrice ?? p.BasePrice) >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => (p.SalePrice ?? p.BasePrice) <= maxPrice.Value);
        }

        // Stock filter
        if (inStockOnly.HasValue && inStockOnly.Value)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        // Featured filter
        if (featuredOnly.HasValue && featuredOnly.Value)
        {
            query = query.Where(p => p.IsFeatured);
        }

        // On sale filter
        if (onSaleOnly.HasValue && onSaleOnly.Value)
        {
            query = query.Where(p => p.SalePrice != null && p.SalePrice < p.BasePrice);
        }

        // Active products only
        query = query.Where(p => p.IsActive);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetNewArrivalsAsync(int withInDays = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-withInDays);
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Images)
            .Where(p => p.IsActive && p.CreatedAt >= startDate)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsOnSaleAsync()
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Images)
            .Where(p => p.IsActive && p.SalePrice != null && p.SalePrice < p.BasePrice)
            .OrderByDescending(p => p.SalePrice)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetCategoriesWithCountAsync()
    {
        var products = await GetProductsWithDetailsAsync();
        return products
            .Where(p => p.IsActive)
            .GroupBy(p => p.CategoryId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<int, int>> GetSuppliersWithCountAsync()
    {
        var products = await GetProductsWithDetailsAsync();
        return products
            .Where(p => p.IsActive && p.SupplierId.HasValue)
            .GroupBy(p => p.SupplierId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
