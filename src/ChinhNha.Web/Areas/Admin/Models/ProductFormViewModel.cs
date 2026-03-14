using System.ComponentModel.DataAnnotations;
using ChinhNha.Application.DTOs.Products;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChinhNha.Web.Areas.Admin.Models;

public class ProductFormViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Mã SKU")]
    public string? SKU { get; set; }
    
    [Display(Name = "Mô tả ngắn")]
    public string? ShortDescription { get; set; }
    
    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }
    
    [Display(Name = "Hướng dẫn sử dụng")]
    public string? UsageInstructions { get; set; }
    
    [Display(Name = "Thông tin kỹ thuật")]
    public string? TechnicalInfo { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }
    
    [Display(Name = "Nhà cung cấp")]
    public int? SupplierId { get; set; }
    
    [Required(ErrorMessage = "Giá nhập là bắt buộc")]
    [Display(Name = "Giá nhập (VND)")]
    public decimal ImportPrice { get; set; }

    [Required(ErrorMessage = "Giá cơ bản là bắt buộc")]
    [Display(Name = "Giá bán (VND)")]
    public decimal BasePrice { get; set; }
    
    [Display(Name = "Giá khuyến mãi (VND)")]
    public decimal? SalePrice { get; set; }
    
    [Display(Name = "Tồn kho")]
    public int StockQuantity { get; set; }
    
    [Display(Name = "Tồn kho tối thiểu (cảnh báo)")]
    public int MinStockLevel { get; set; }
    
    [Required(ErrorMessage = "Đơn vị tính là bắt buộc")]
    [Display(Name = "Đơn vị tính (kg, lít, chai...)")]
    public string Unit { get; set; } = "kg";
    
    [Display(Name = "Trọng lượng")]
    public decimal? Weight { get; set; }
    
    [Display(Name = "Sản phẩm nổi bật?")]
    public bool IsFeatured { get; set; }
    
    [Display(Name = "Kích hoạt (Hiển thị)?")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Link NSX")]
    public string? ManufacturerUrl { get; set; }
    
    [Display(Name = "Hình ảnh (URL)")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Upload hình ảnh")]
    public IFormFile? ImageFile { get; set; }

    // Dropdowns
    public IEnumerable<SelectListItem>? Categories { get; set; }
    public IEnumerable<SelectListItem>? Suppliers { get; set; }

    // Variants (populated only in Edit)
    public List<ProductVariantDto> Variants { get; set; } = new();
}
