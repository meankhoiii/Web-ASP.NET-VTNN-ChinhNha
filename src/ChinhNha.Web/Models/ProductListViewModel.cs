using ChinhNha.Application.DTOs.Products;
using ChinhNha.Domain.Entities;

namespace ChinhNha.Web.Models;

public class ProductListViewModel
{
    public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
    public IEnumerable<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? SearchQuery { get; set; }
    public string? CategorySlug { get; set; }
}
