using AutoMapper;
using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChinhNha.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IAppUserService _appUserService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProductService> _logger;
    private readonly IMapper _mapper;

    public ProductService(
        IProductRepository productRepository,
        IWishlistRepository wishlistRepository,
        IAppUserService appUserService,
        IEmailService emailService,
        ILogger<ProductService> logger,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _wishlistRepository = wishlistRepository;
        _appUserService = appUserService;
        _emailService = emailService;
        _logger = logger;
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

        var previousEffectivePrice = product.SalePrice ?? product.BasePrice;
        var newEffectivePrice = request.SalePrice ?? request.BasePrice;

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

        if (newEffectivePrice < previousEffectivePrice)
        {
            await SendWishlistPriceDropAlertsAsync(product, previousEffectivePrice, newEffectivePrice);
        }

        return true;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return false;

        await _productRepository.DeleteAsync(product);
        return true;
    }

    private async Task SendWishlistPriceDropAlertsAsync(Product product, decimal oldPrice, decimal newPrice)
    {
        try
        {
            var watchItems = await _wishlistRepository.GetWishlistsByProductAsync(product.Id);
            if (!watchItems.Any())
                return;

            var uniqueUserIds = watchItems
                .Select(w => w.UserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (!uniqueUserIds.Any())
                return;

            foreach (var userId in uniqueUserIds)
            {
                var user = await _appUserService.GetUserByIdAsync(userId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    continue;

                var subject = $"[ChinhNha] San pham '{product.Name}' dang giam gia";
                var htmlBody = $@"
<div style='font-family:Arial,sans-serif;line-height:1.6'>
    <h3 style='color:#1b5e20'>Thong bao giam gia tu ChinhNha</h3>
    <p>San pham trong wishlist cua ban vua duoc dieu chinh gia:</p>
    <ul>
        <li><strong>San pham:</strong> {product.Name}</li>
        <li><strong>Gia cu:</strong> {oldPrice:N0}đ</li>
        <li><strong>Gia moi:</strong> {newPrice:N0}đ</li>
    </ul>
    <p>Ban co the dang nhap de dat hang ngay tai cua hang ChinhNha.</p>
</div>";

                await _emailService.SendAsync(user.Email, subject, htmlBody);
            }

            _logger.LogInformation(
                "Wishlist sale alerts sent for product {ProductId} to {UserCount} users",
                product.Id,
                uniqueUserIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending wishlist sale alerts for product {ProductId}", product.Id);
        }
    }
}
