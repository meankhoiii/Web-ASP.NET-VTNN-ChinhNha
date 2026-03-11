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
}
