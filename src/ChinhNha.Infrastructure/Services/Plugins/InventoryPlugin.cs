using System.ComponentModel;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace ChinhNha.Infrastructure.Services.Plugins;

/// <summary>
/// Plugin kiểm tra tồn kho và gợi ý sản phẩm cho chatbot.
/// Các hàm này được Semantic Kernel tự động gọi khi LLM cần thông tin kho/sản phẩm.
/// </summary>
public class InventoryPlugin
{
    private readonly AppDbContext _context;

    public InventoryPlugin(AppDbContext context)
    {
        _context = context;
    }

    [KernelFunction, Description("Kiểm tra số lượng tồn kho của sản phẩm theo tên hoặc từ khóa.")]
    public async Task<string> CheckStockByNameAsync(
        [Description("Tên sản phẩm hoặc từ khóa cần tra cứu, ví dụ: 'phân urê', 'NPK 16-16-8'")] string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return "Vui lòng cung cấp tên sản phẩm cần kiểm tra.";

        var keyword = productName.ToLower();
        var products = await _context.Products
            .Where(p => p.IsActive && p.Name.ToLower().Contains(keyword))
            .Select(p => new { p.Name, p.StockQuantity, p.Unit, p.MinStockLevel })
            .Take(5)
            .ToListAsync();

        if (!products.Any())
            return $"Không tìm thấy sản phẩm nào khớp với '{productName}'.";

        var lines = products.Select(p =>
        {
            var status = p.StockQuantity <= p.MinStockLevel ? "⚠️ sắp hết" : "✅ còn hàng";
            return $"- {p.Name}: còn {p.StockQuantity} {p.Unit} ({status})";
        });
        return string.Join("\n", lines);
    }

    [KernelFunction, Description("Lấy giá và thông tin sản phẩm theo tên.")]
    public async Task<string> GetProductInfoAsync(
        [Description("Tên sản phẩm cần tra giá và thông tin")] string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return "Vui lòng cung cấp tên sản phẩm.";

        var keyword = productName.ToLower();
        var products = await _context.Products
            .Where(p => p.IsActive && p.Name.ToLower().Contains(keyword))
            .Select(p => new { p.Name, p.BasePrice, p.SalePrice, p.Unit, p.Description, p.StockQuantity })
            .Take(3)
            .ToListAsync();

        if (!products.Any())
            return $"Không tìm thấy sản phẩm '{productName}'.";

        var lines = products.Select(p =>
        {
            var price = p.SalePrice.HasValue && p.SalePrice.Value < p.BasePrice
                ? $"**{p.SalePrice.Value:N0}đ** (giảm từ {p.BasePrice:N0}đ)"
                : $"{p.BasePrice:N0}đ";
            var stock = p.StockQuantity > 0 ? "còn hàng" : "hết hàng";
            return $"- **{p.Name}**: {price}/{p.Unit} — {stock}";
        });
        return "Thông tin sản phẩm:\n" + string.Join("\n", lines);
    }

    [KernelFunction, Description("Gợi ý sản phẩm phân bón/vật tư nông nghiệp phù hợp theo mô tả nhu cầu hoặc loại cây trồng.")]
    public async Task<string> SuggestProductsAsync(
        [Description("Mô tả nhu cầu, ví dụ: 'phân bón cho lúa', 'thuốc trừ sâu', 'phân NPK'")] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Vui lòng mô tả nhu cầu của bạn.";

        var keyword = query.ToLower();
        var products = await _context.Products
            .Where(p => p.IsActive && p.StockQuantity > 0 &&
                        (p.Name.ToLower().Contains(keyword) ||
                         (p.Description != null && p.Description.ToLower().Contains(keyword))))
            .Select(p => new { p.Name, p.BasePrice, p.SalePrice, p.Unit })
            .Take(5)
            .ToListAsync();

        if (!products.Any())
            return "Hiện tại chúng tôi chưa có sản phẩm phù hợp với yêu cầu. Vui lòng liên hệ nhân viên để được tư vấn thêm.";

        var lines = products.Select(p =>
        {
            var price = p.SalePrice ?? p.BasePrice;
            return $"- {p.Name}: {price:N0}đ/{p.Unit}";
        });
        return "Gợi ý sản phẩm phù hợp:\n" + string.Join("\n", lines) + "\n\nBạn cần tư vấn thêm về sản phẩm nào không?";
    }
}
