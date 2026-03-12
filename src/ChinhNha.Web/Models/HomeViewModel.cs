using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.DTOs.Blogs;
using ChinhNha.Domain.Entities;

namespace ChinhNha.Web.Models;

public class HomeViewModel
{
    public IEnumerable<ProductDto> FeaturedProducts { get; set; } = new List<ProductDto>();
    public IEnumerable<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
    public IEnumerable<BlogPostDto> LatestBlogs { get; set; } = new List<BlogPostDto>();
}
