using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Web.Models;

public class ProductDetailsViewModel
{
    public ProductDto Product { get; set; } = null!;
    public IEnumerable<ProductDto> RelatedProducts { get; set; } = new List<ProductDto>();
    public IEnumerable<ProductReviewDto> Reviews { get; set; } = new List<ProductReviewDto>();
    public ProductReviewStatsDto? ReviewStats { get; set; }
}
