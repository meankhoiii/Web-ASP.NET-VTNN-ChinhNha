using System.ComponentModel.DataAnnotations;

namespace ChinhNha.Web.Areas.Admin.Models;

public class SupplierFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
    [Display(Name = "Tên nhà cung cấp")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Người liên hệ")]
    public string? ContactPerson { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Website")]
    public string? Website { get; set; }

    [Range(0, 365, ErrorMessage = "Lead time phải từ 0 đến 365 ngày")]
    [Display(Name = "Lead time (ngày)")]
    public int LeadTimeDays { get; set; } = 3;

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; } = true;
}
