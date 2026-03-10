namespace ChinhNha.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,      // Chờ xác nhận
    Confirmed = 1,    // Đã xác nhận
    Processing = 2,   // Đang chuẩn bị hàng
    Shipping = 3,     // Đang giao hàng
    Delivered = 4,    // Đã giao thành công
    Cancelled = 5,    // Đã hủy
    Returned = 6      // Đã trả hàng
}
