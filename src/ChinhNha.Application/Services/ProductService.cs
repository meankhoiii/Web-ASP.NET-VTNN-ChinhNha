using AutoMapper;
using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _productRepository.GetProductsWithDetailsAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync()
    {
        var products = await _productRepository.GetFeaturedProductsAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _productRepository.GetProductWithDetailsByIdAsync(id);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug)
    {
        var product = await _productRepository.GetProductWithDetailsBySlugAsync(slug);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId)
    {
        var products = await _productRepository.GetProductsByCategoryAsync(categoryId);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategorySlugAsync(string categorySlug)
    {
        var products = await _productRepository.GetProductsByCategorySlugAsync(categorySlug);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<int> CreateProductAsync(ChinhNha.Application.DTOs.Requests.CreateProductRequest request)
    {
        var slug = ChinhNha.Application.Helpers.SlugHelper.GenerateSlug(request.Name);
        var product = new ChinhNha.Domain.Entities.Product
        {
            Name = request.Name,
            Slug = slug,
            SKU = request.SKU,
            ShortDescription = request.ShortDescription,
            Description = request.Description,
            UsageInstructions = request.UsageInstructions,
            TechnicalInfo = request.TechnicalInfo,
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId,
            BasePrice = request.BasePrice,
            SalePrice = request.SalePrice,
            StockQuantity = request.StockQuantity,
            MinStockLevel = request.MinStockLevel,
            Unit = request.Unit,
            Weight = request.Weight,
            IsFeatured = request.IsFeatured,
            IsActive = request.IsActive,
            ManufacturerUrl = request.ManufacturerUrl,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(request.InitialImageUrl))
        {
            product.Images.Add(new ChinhNha.Domain.Entities.ProductImage
            {
                ImageUrl = request.InitialImageUrl,
                IsPrimary = true,
                DisplayOrder = 1
            });
        }

        await _productRepository.AddAsync(product);
        return product.Id;
    }

    public async Task<bool> UpdateProductAsync(ChinhNha.Application.DTOs.Requests.UpdateProductRequest request)
    {
        var product = await _productRepository.GetProductWithDetailsByIdAsync(request.Id);
        if (product == null) return false;

        product.Name = request.Name;
        // Don't auto update slug once created to avoid breaking SEO, unless needed. We'll update if empty.
        if (string.IsNullOrEmpty(product.Slug))
        {
            product.Slug = ChinhNha.Application.Helpers.SlugHelper.GenerateSlug(request.Name);
        }
        
        product.SKU = request.SKU;
        product.ShortDescription = request.ShortDescription;
        product.Description = request.Description;
        product.UsageInstructions = request.UsageInstructions;
        product.TechnicalInfo = request.TechnicalInfo;
        product.CategoryId = request.CategoryId;
        product.SupplierId = request.SupplierId;
        product.BasePrice = request.BasePrice;
        product.SalePrice = request.SalePrice;
        product.StockQuantity = request.StockQuantity;
        product.MinStockLevel = request.MinStockLevel;
        product.Unit = request.Unit;
        product.Weight = request.Weight;
        product.IsFeatured = request.IsFeatured;
        product.IsActive = request.IsActive;
        product.ManufacturerUrl = request.ManufacturerUrl;
        product.MetaTitle = request.MetaTitle;
        product.MetaDescription = request.MetaDescription;
        product.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.NewImageUrl))
        {
            foreach (var img in product.Images) img.IsPrimary = false; // Deflag old primary
            
            product.Images.Add(new ChinhNha.Domain.Entities.ProductImage
            {
                ImageUrl = request.NewImageUrl,
                IsPrimary = true,
                DisplayOrder = 0
            });
        }

        await _productRepository.UpdateAsync(product);
        return true;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return false;

        await _productRepository.DeleteAsync(product);
        return true;
    }
}
