using System.ComponentModel.DataAnnotations;

namespace ChinhNha.Web.Areas.Admin.Models;

public class SaveVariantRequest
{
    public int Id { get; set; }  // 0 = create, > 0 = update
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Tên biến thể là bắt buộc")]
    public string VariantName { get; set; } = string.Empty;

    public string? SKU { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá không hợp lệ")]
    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public int StockQuantity { get; set; }

    public decimal? Weight { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }
}
