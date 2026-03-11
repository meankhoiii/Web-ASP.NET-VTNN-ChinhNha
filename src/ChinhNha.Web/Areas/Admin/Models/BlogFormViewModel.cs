using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChinhNha.Web.Areas.Admin.Models;

public class BlogFormViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [Display(Name = "Tiêu đề bài viết")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung là bắt buộc")]
    [Display(Name = "Nội dung")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Mô tả ngắn (Trích dẫn)")]
    public string? Excerpt { get; set; }

    [Display(Name = "Hình ảnh đại diện (URL)")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Chọn danh mục")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Display(Name = "Xuất bản ngay?")]
    public bool IsPublished { get; set; }

    [Display(Name = "SEO Title")]
    public string? MetaTitle { get; set; }

    [Display(Name = "SEO Description")]
    public string? MetaDescription { get; set; }

    public IEnumerable<SelectListItem>? Categories { get; set; }
}
