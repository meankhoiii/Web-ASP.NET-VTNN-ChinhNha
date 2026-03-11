using System.ComponentModel.DataAnnotations;

namespace ChinhNha.Web.Models.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Nhớ tôi?")]
    public bool RememberMe { get; set; }
    
    public string? ReturnUrl { get; set; }
}
