using System.ComponentModel;
using ChinhNha.Domain.Enums;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace ChinhNha.Infrastructure.Services.Plugins;

/// <summary>
/// Plugin báo cáo dành riêng cho admin chatbot.
/// Cung cấp tóm tắt doanh thu, tồn kho thấp, sản phẩm bán chạy.
/// </summary>
public class ReportPlugin
{
    private readonly AppDbContext _context;

    public ReportPlugin(AppDbContext context)
    {
        _context = context;
    }

    [KernelFunction, Description("Lấy tóm tắt doanh thu và đơn hàng trong N ngày gần đây. Dùng cho báo cáo admin.")]
    public async Task<string> GetSalesSummaryAsync(
        [Description("Số ngày muốn xem báo cáo, ví dụ: 7, 30")] int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var completedStatuses = new[] { OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipping, OrderStatus.Delivered };

        var orders = await _context.Orders
            .Where(o => o.OrderDate >= since)
            .ToListAsync();

        var totalOrders = orders.Count;
        var completedOrders = orders.Count(o => completedStatuses.Contains(o.Status));
        var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);
        var revenue = orders
            .Where(o => completedStatuses.Contains(o.Status))
            .Sum(o => o.TotalAmount);

        return $"📊 Báo cáo {days} ngày gần đây:\n" +
               $"- Tổng đơn hàng: {totalOrders}\n" +
               $"- Đơn thành công: {completedOrders}\n" +
               $"- Đơn huỷ: {cancelledOrders}\n" +
               $"- Doanh thu: {revenue:N0}đ";
    }

    [KernelFunction, Description("Lấy danh sách sản phẩm sắp hết hàng (tồn kho dưới mức tối thiểu). Dùng cho quản lý kho.")]
    public async Task<string> GetLowStockProductsAsync()
    {
        var lowStock = await _context.Products
            .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new { p.Name, p.StockQuantity, p.MinStockLevel, p.Unit })
            .Take(10)
            .ToListAsync();

        if (!lowStock.Any())
            return "✅ Tất cả sản phẩm đều còn đủ hàng trong kho.";

        var lines = lowStock.Select(p =>
            $"- ⚠️ {p.Name}: còn {p.StockQuantity}/{p.MinStockLevel} {p.Unit}");

        return $"Có {lowStock.Count} sản phẩm sắp hết hàng:\n" + string.Join("\n", lines);
    }

    [KernelFunction, Description("Lấy top sản phẩm bán chạy nhất theo số lượng đơn thành công.")]
    public async Task<string> GetTopSellingProductsAsync(
        [Description("Số lượng sản phẩm muốn hiển thị, mặc định 5")] int top = 5)
    {
        var completedStatuses = new[] { OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipping, OrderStatus.Delivered };

        var topProducts = await _context.OrderItems
            .Include(i => i.Order)
            .Where(i => completedStatuses.Contains(i.Order.Status))
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                g.Key.ProductName,
                TotalSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(top)
            .ToListAsync();

        if (!topProducts.Any())
            return "Chưa có dữ liệu đơn hàng thành công.";

        var lines = topProducts.Select((p, i) =>
            $"{i + 1}. {p.ProductName}: {p.TotalSold} đơn — Doanh thu: {p.Revenue:N0}đ");

        return $"🏆 Top {top} sản phẩm bán chạy:\n" + string.Join("\n", lines);
    }
}
