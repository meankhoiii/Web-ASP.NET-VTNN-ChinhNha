using System.ComponentModel.DataAnnotations;

namespace ChinhNha.Web.Models.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Họ tên.")]
    [StringLength(100, ErrorMessage = "{0} phải từ {2} đến {1} ký tự.", MinimumLength = 3)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Email hoặc SĐT.")]
    [RegularExpression(
        @"^([^@\s]+@[^@\s]+\.[^@\s]+|(\+?84|0)[0-9\.\-\s]{8,12})$",
        ErrorMessage = "Vui lòng nhập đúng định dạng Email hoặc SĐT.")]
    [Display(Name = "Địa chỉ Email (hoặc SĐT)")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Mật khẩu.")]
    [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
