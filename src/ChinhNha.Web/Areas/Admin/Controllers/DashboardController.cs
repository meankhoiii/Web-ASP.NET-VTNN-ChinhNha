using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Enums;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class DashboardController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IInventoryForecastService _forecastService;

    public DashboardController(
        IOrderService orderService,
        IProductService productService,
        IInventoryForecastService forecastService)
    {
        _orderService = orderService;
        _productService = productService;
        _forecastService = forecastService;
    }

    public async Task<IActionResult> Index()
    {
        // Avoid concurrent queries on the same scoped DbContext.
        var orders = (await _orderService.GetAllOrdersAsync())
            .OrderByDescending(o => o.OrderDate)
            .ToList();
        var products = (await _productService.GetAllProductsAsync())
            .Where(p => p.IsActive)
            .ToList();

        var completedStatuses = new[] { OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipping, OrderStatus.Delivered };
        var revenueOrders = orders.Where(o => completedStatuses.Contains(o.Status));
        var deliveredRevenue = revenueOrders.Sum(o => o.TotalAmount);

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var newOrdersCount = orders.Count(o => o.OrderDate >= sevenDaysAgo);

        var lowStockProducts = products
            .Where(p => p.StockQuantity <= p.MinStockLevel)
            .OrderBy(p => p.StockQuantity - p.MinStockLevel)
            .Take(8)
            .Select(p => new LowStockProductViewModel
            {
                ProductId = p.Id,
                ProductName = p.Name,
                StockQuantity = p.StockQuantity,
                MinStockLevel = p.MinStockLevel
            })
            .ToList();

        var recentOrders = orders
            .Take(6)
            .Select(o => new RecentOrderViewModel
            {
                OrderId = o.Id,
                CustomerName = string.IsNullOrWhiteSpace(o.UserFullName) ? "Khach hang" : o.UserFullName,
                OrderDate = o.OrderDate.ToLocalTime(),
                StatusLabel = o.Status.ToString(),
                TotalAmount = o.TotalAmount
            })
            .ToList();

        var topSellingProducts = orders
            .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopSellingProductViewModel
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                SoldQuantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.SoldQuantity)
            .Take(5)
            .ToList();

        var forecastProductIds = topSellingProducts
            .Select(x => x.ProductId)
            .Concat(products.OrderByDescending(p => p.ViewCount).Select(p => p.Id))
            .Distinct()
            .Take(3)
            .ToList();

        string BuildTrendLabel(IReadOnlyList<decimal> values)
        {
            if (values.Count < 2)
            {
                return "Ổn định";
            }

            var first = values.First();
            if (first == 0)
            {
                return values.Last() == 0 ? "Ổn định" : "Tăng nhẹ";
            }

            var deltaPercent = (values.Last() - first) / first;
            if (deltaPercent >= 0.08m)
            {
                return "Tăng";
            }

            if (deltaPercent >= 0.02m)
            {
                return "Tăng nhẹ";
            }

            if (deltaPercent <= -0.08m)
            {
                return "Giảm";
            }

            if (deltaPercent <= -0.02m)
            {
                return "Giảm nhẹ";
            }

            return "Ổn định";
        }

        var chartMetas = new[]
        {
            new { Title = "Doanh thu dự báo", Dataset = "Doanh thu", YAxis = "Đồng", Unit = "đ" },
            new { Title = "Số đơn hàng dự báo", Dataset = "Đơn hàng", YAxis = "Đơn hàng", Unit = "đơn" },
            new { Title = "Sản phẩm bán ra dự báo", Dataset = "Sản phẩm", YAxis = "Sản phẩm", Unit = "sản phẩm" }
        };

        var miniCharts = new List<ForecastMiniChartViewModel>();
        for (var i = 0; i < forecastProductIds.Count; i++)
        {
            var productId = forecastProductIds[i];
            try
            {
                var forecastData = (await _forecastService.GetForecastForProductAsync(productId, 6))
                    .Where(x => !x.IsHistorical)
                    .OrderBy(x => x.TargetDate)
                    .ToList();

                if (!forecastData.Any())
                {
                    continue;
                }

                var values = forecastData.Select(x => Math.Round(x.PredictedDemand, 2)).ToList();
                var chartMeta = chartMetas[Math.Min(i, chartMetas.Length - 1)];

                miniCharts.Add(new ForecastMiniChartViewModel
                {
                    ProductId = productId,
                    ProductName = forecastData.First().ProductName,
                    ChartTitle = chartMeta.Title,
                    DatasetLabel = chartMeta.Dataset,
                    YAxisLabel = chartMeta.YAxis,
                    UnitLabel = chartMeta.Unit,
                    TrendLabel = BuildTrendLabel(values),
                    Labels = forecastData.Select(x => x.TargetDate.ToLocalTime().ToString("dd/MM")).ToList(),
                    Values = values
                });
            }
            catch
            {
                // Model AI có thể chưa được train cho một số sản phẩm.
            }
        }

        var model = new DashboardViewModel
        {
            NewOrdersCount = newOrdersCount,
            DeliveredRevenue = deliveredRevenue,
            LowStockCount = products.Count(p => p.StockQuantity <= p.MinStockLevel),
            TotalProducts = products.Count,
            LowStockProducts = lowStockProducts,
            RecentOrders = recentOrders,
            TopSellingProducts = topSellingProducts,
            ForecastMiniCharts = miniCharts
        };

        if (model.LowStockCount > 0)
        {
            model.AdminAlerts.Add($"Canh bao: co {model.LowStockCount} san pham dang o muc ton kho thap.");
            foreach (var p in model.LowStockProducts.Take(3))
            {
                model.AdminAlerts.Add($"{p.ProductName}: ton {p.StockQuantity}, muc toi thieu {p.MinStockLevel}.");
            }
        }
        else
        {
            model.AdminAlerts.Add("Khong co canh bao ton kho thap trong he thong.");
        }

        return View(model);
    }
}
