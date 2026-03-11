using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Web.Models;

public class HomeViewModel
{
    public IEnumerable<ProductDto> FeaturedProducts { get; set; } = new List<ProductDto>();
    // Future: Banners, News, Bestsellers, etc.
}
