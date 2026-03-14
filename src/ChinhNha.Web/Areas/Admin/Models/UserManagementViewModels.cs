using System.ComponentModel.DataAnnotations;
using ChinhNha.Application.DTOs.Admin;

namespace ChinhNha.Web.Areas.Admin.Models;

public class UserFilterViewModel
{
    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public string? ActiveStatus { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}

public class UserListViewModel
{
    public UserStatsDto Stats { get; set; } = new();
    public UserFilterViewModel Filter { get; set; } = new();
}

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^(0\d{9}|0\d{2}\.\d{3}\.\d{4})$", ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam")]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string Role { get; set; } = "Customer";

    public bool IsActive { get; set; } = true;

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    public string? AvatarUrl { get; set; }

    public IFormFile? AvatarFile { get; set; }

    [MinLength(8, ErrorMessage = "Mật khẩu phải tối thiểu 8 ký tự")]
    public string? Password { get; set; }

    public string? ConfirmPassword { get; set; }
}

public class UserDetailViewModel
{
    public UserDetailDto Detail { get; set; } = new();
}
