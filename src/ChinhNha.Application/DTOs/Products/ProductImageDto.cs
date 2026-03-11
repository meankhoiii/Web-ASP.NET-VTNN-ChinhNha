namespace ChinhNha.Application.DTOs.Products;

public class ProductImageDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}
