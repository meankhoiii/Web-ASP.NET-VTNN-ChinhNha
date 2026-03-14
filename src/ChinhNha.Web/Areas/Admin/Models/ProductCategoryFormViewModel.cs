using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChinhNha.Web.Areas.Admin.Models;

public class ProductCategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Ảnh đại diện (URL)")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Danh mục cha")]
    public int? ParentCategoryId { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem>? ParentCategories { get; set; }
}
