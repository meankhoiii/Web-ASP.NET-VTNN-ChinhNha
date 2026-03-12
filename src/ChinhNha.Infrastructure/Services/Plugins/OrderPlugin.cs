using System.ComponentModel;
using ChinhNha.Domain.Enums;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace ChinhNha.Infrastructure.Services.Plugins;

/// <summary>
/// Plugin tra cứu trạng thái đơn hàng cho chatbot khách hàng.
/// </summary>
public class OrderPlugin
{
    private readonly AppDbContext _context;

    public OrderPlugin(AppDbContext context)
    {
        _context = context;
    }

    [KernelFunction, Description("Tra cứu trạng thái đơn hàng theo mã đơn hàng (ID).")]
    public async Task<string> GetOrderStatusAsync(
        [Description("Mã đơn hàng (số nguyên), ví dụ: 1234")] int orderId)
    {
        var order = await _context.Orders
            .Where(o => o.Id == orderId)
                .Select(o => new { o.Id, o.Status, o.TotalAmount, o.OrderDate, o.ReceiverName })
            .FirstOrDefaultAsync();

        if (order == null)
            return $"Không tìm thấy đơn hàng #{orderId}. Vui lòng kiểm tra lại mã đơn.";

        var statusVi = order.Status switch
        {
            OrderStatus.Pending     => "Chờ xác nhận",
            OrderStatus.Confirmed   => "Đã xác nhận",
            OrderStatus.Processing  => "Đang xử lý",
            OrderStatus.Shipping    => "Đang giao hàng",
            OrderStatus.Delivered   => "Đã giao thành công",
            OrderStatus.Cancelled   => "Đã huỷ",
            OrderStatus.Returned    => "Đã hoàn trả",
            _                       => order.Status.ToString()
        };

        return $"📦 Đơn hàng #{order.Id}\n" +
                   $"- Người nhận: {order.ReceiverName}\n" +
               $"- Ngày đặt: {order.OrderDate.ToLocalTime():dd/MM/yyyy HH:mm}\n" +
               $"- Tổng tiền: {order.TotalAmount:N0}đ\n" +
               $"- Trạng thái: **{statusVi}**";
    }
}
