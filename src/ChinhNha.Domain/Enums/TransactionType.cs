namespace ChinhNha.Domain.Enums;

public enum TransactionType
{
    Import = 0,     // Nhập kho
    Export = 1,     // Xuất kho (bán)
    Return = 2,     // Khách trả hàng
    Adjustment = 3, // Điều chỉnh kiểm kê
    Loss = 4        // Hao hụt
}
