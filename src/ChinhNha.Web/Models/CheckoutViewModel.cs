using ChinhNha.Application.DTOs.Orders;
using System.ComponentModel.DataAnnotations;

namespace ChinhNha.Web.Models;

public class CheckoutViewModel
{
    public CartDto Cart { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    [Display(Name = "Họ và tên")]
    public string ReceiverName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
    [Display(Name = "Địa chỉ nhận hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Display(Name = "Ghi chú đơn hàng")]
    public string? Note { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "COD"; // COD or VNPay
}
