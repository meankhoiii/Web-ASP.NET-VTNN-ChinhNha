using AutoMapper;
using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.Services;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChinhNha.Application.Tests;

public class WishlistServiceTests
{
    private readonly Mock<IWishlistRepository> _wishlistRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ILogger<WishlistService>> _logger = new();

    [Fact]
    public async Task AddToWishlistAsync_UsesSalePriceAndReturnsProductData()
    {
        var product = CreateProduct(42, 120000m, 99000m);
        var wishlist = new Wishlist
        {
            Id = 7,
            UserId = "user-1",
            ProductId = product.Id,
            AddedAt = DateTime.UtcNow,
            PriceWhenAdded = product.SalePrice
        };

        _productRepository.Setup(repo => repo.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _wishlistRepository
            .Setup(repo => repo.AddToWishlistAsync("user-1", product.Id, 99000m, "Can mua dot sau"))
            .ReturnsAsync(wishlist);

        var service = CreateService();
        var request = new CreateWishlistDto
        {
            ProductId = product.Id,
            WishlistName = "Mua vu he thu",
            Notes = "Can mua dot sau",
            Priority = 5
        };

        var result = await service.AddToWishlistAsync("user-1", request);

        Assert.Equal(product.Id, result.ProductId);
        Assert.Equal(product.Name, result.ProductName);
        Assert.Equal(product.BasePrice, result.ProductPrice);
        Assert.Equal(product.SalePrice, result.ProductSalePrice);
        Assert.Equal(product.Images.First().ImageUrl, result.ProductImage);
        Assert.Equal(99000m, result.PriceWhenAdded);
    }

    [Fact]
    public async Task MarkAsPurchasedAsync_UpdatesWishlistItemWhenProductExists()
    {
        var wishlist = new Wishlist
        {
            Id = 10,
            UserId = "user-2",
            ProductId = 88,
            AddedAt = DateTime.UtcNow.AddDays(-2)
        };

        _wishlistRepository
            .Setup(repo => repo.GetUserWishlistsAsync("user-2"))
            .ReturnsAsync(new[] { wishlist });

        Wishlist? updatedItem = null;
        _wishlistRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<Wishlist>()))
            .Callback<Wishlist>(entity => updatedItem = entity)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var result = await service.MarkAsPurchasedAsync("user-2", 88);

        Assert.True(result);
        Assert.NotNull(updatedItem);
        Assert.NotNull(updatedItem!.PurchasedAt);
        _wishlistRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Wishlist>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsPurchasedAsync_ReturnsFalseWhenWishlistItemMissing()
    {
        _wishlistRepository
            .Setup(repo => repo.GetUserWishlistsAsync("user-3"))
            .ReturnsAsync(Array.Empty<Wishlist>());

        var service = CreateService();

        var result = await service.MarkAsPurchasedAsync("user-3", 999);

        Assert.False(result);
        _wishlistRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Wishlist>()), Times.Never);
    }

    private WishlistService CreateService()
    {
        return new WishlistService(
            _wishlistRepository.Object,
            _productRepository.Object,
            _mapper.Object,
            _logger.Object);
    }

    private static Product CreateProduct(int id, decimal basePrice, decimal? salePrice)
    {
        return new Product
        {
            Id = id,
            Name = "Phan bon huu co",
            Slug = "phan-bon-huu-co",
            BasePrice = basePrice,
            SalePrice = salePrice,
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "/images/products/test.jpg", IsPrimary = true }
            }
        };
    }
}