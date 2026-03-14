using System.ComponentModel.DataAnnotations;

namespace ChinhNha.Web.Models.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Email hoặc SĐT.")]
    [RegularExpression(
        @"^([^@\s]+@[^@\s]+\.[^@\s]+|(\+?84|0)[0-9\.\-\s]{8,12})$",
        ErrorMessage = "Vui lòng nhập đúng định dạng Email hoặc SĐT.")]
    [Display(Name = "Địa chỉ Email (hoặc SĐT)")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Nhớ tôi?")]
    public bool RememberMe { get; set; }
    
    public string? ReturnUrl { get; set; }
}
