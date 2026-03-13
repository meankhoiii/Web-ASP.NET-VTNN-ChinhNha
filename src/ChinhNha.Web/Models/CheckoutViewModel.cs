using ChinhNha.Application.DTOs.Orders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace ChinhNha.Web.Models;

public class CheckoutViewModel
{
    [ValidateNever]
    public CartDto Cart { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    [Display(Name = "Họ và tên")]
    public string ReceiverName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string ReceiverPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email nhận xác nhận (không bắt buộc)")]
    public string? ReceiverEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")]
    [Display(Name = "Tỉnh/Thành phố")]
    public string ShippingProvince { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện.")]
    [Display(Name = "Quận/Huyện")]
    public string ShippingDistrict { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn Phường/Xã.")]
    [Display(Name = "Phường/Xã")]
    public string ShippingWard { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết (số nhà, ấp/thôn...).")]
    [Display(Name = "Địa chỉ chi tiết")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Display(Name = "Ghi chú đơn hàng")]
    public string? Note { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "COD"; // COD or VNPay
}
