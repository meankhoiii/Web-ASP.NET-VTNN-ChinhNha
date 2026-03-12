using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetProductsWithDetailsAsync();
    Task<Product?> GetProductWithDetailsByIdAsync(int id);
    Task<Product?> GetProductWithDetailsBySlugAsync(string slug);
    Task<IEnumerable<Product>> GetFeaturedProductsAsync();
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> GetProductsByCategorySlugAsync(string categorySlug);
    
    // Advanced search methods
    Task<IEnumerable<Product>> SearchProductsAsync(
        string? searchQuery = null,
        int? categoryId = null,
        int? supplierId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStockOnly = null,
        bool? featuredOnly = null,
        bool? onSaleOnly = null);
    
    Task<IEnumerable<Product>> GetNewArrivalsAsync(int withInDays = 30);
    Task<IEnumerable<Product>> GetProductsOnSaleAsync();
    Task<Dictionary<int, int>> GetCategoriesWithCountAsync();
    Task<Dictionary<int, int>> GetSuppliersWithCountAsync();
}
