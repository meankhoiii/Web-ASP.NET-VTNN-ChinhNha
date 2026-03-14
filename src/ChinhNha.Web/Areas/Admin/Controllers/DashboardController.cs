using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Enums;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

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

    public async Task<IActionResult> Index(
        string preset = "30d",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? orderStatus = null,
        string paymentState = "all")
    {
        // Avoid concurrent queries on the same scoped DbContext.
        var allOrders = (await _orderService.GetAllOrdersAsync())
            .OrderByDescending(o => o.OrderDate)
            .ToList();
        var products = (await _productService.GetAllProductsAsync())
            .Where(p => p.IsActive)
            .ToList();

        var normalizedPreset = string.IsNullOrWhiteSpace(preset)
            ? "30d"
            : preset.Trim().ToLowerInvariant();
        var normalizedPaymentState = string.IsNullOrWhiteSpace(paymentState)
            ? "all"
            : paymentState.Trim().ToLowerInvariant();

        var nowLocal = DateTime.Now;

        DateTime? startLocal = null;
        DateTime? endLocal = null;

        switch (normalizedPreset)
        {
            case "7d":
                endLocal = nowLocal;
                startLocal = nowLocal.Date.AddDays(-6);
                break;
            case "30d":
                endLocal = nowLocal;
                startLocal = nowLocal.Date.AddDays(-29);
                break;
            case "90d":
                endLocal = nowLocal;
                startLocal = nowLocal.Date.AddDays(-89);
                break;
            case "ytd":
                endLocal = nowLocal;
                startLocal = new DateTime(nowLocal.Year, 1, 1);
                break;
            case "all":
                break;
            case "custom":
                if (fromDate.HasValue)
                {
                    startLocal = fromDate.Value.Date;
                }

                if (toDate.HasValue)
                {
                    endLocal = toDate.Value.Date.AddDays(1).AddTicks(-1);
                }
                break;
            default:
                normalizedPreset = "30d";
                endLocal = nowLocal;
                startLocal = nowLocal.Date.AddDays(-29);
                break;
        }

        var filterOrderStatus = !string.IsNullOrWhiteSpace(orderStatus)
            && Enum.TryParse<OrderStatus>(orderStatus, true, out var parsedStatus)
            ? parsedStatus
            : (OrderStatus?)null;

        var filteredOrders = allOrders.Where(o =>
        {
            var localDate = o.OrderDate.ToLocalTime();

            if (startLocal.HasValue && localDate < startLocal.Value)
            {
                return false;
            }

            if (endLocal.HasValue && localDate > endLocal.Value)
            {
                return false;
            }

            if (filterOrderStatus.HasValue && o.Status != filterOrderStatus.Value)
            {
                return false;
            }

            if (normalizedPaymentState == "paid")
            {
                var isPaid = o.IsPaid || string.Equals(o.PaymentStatus, PaymentStatus.Paid.ToString(), StringComparison.OrdinalIgnoreCase);
                if (!isPaid)
                {
                    return false;
                }
            }
            else if (normalizedPaymentState == "unpaid")
            {
                var isPaid = o.IsPaid || string.Equals(o.PaymentStatus, PaymentStatus.Paid.ToString(), StringComparison.OrdinalIgnoreCase);
                if (isPaid)
                {
                    return false;
                }
            }

            return true;
        }).ToList();

        var newOrdersCount = filteredOrders.Count;

        var paidRevenueFiltered = filteredOrders
            .Where(o => o.IsPaid || string.Equals(o.PaymentStatus, PaymentStatus.Paid.ToString(), StringComparison.OrdinalIgnoreCase))
            .Sum(o => o.TotalAmount);

        // In test VNPay/COD flows, payment status may remain pending even when orders are operational.
        // Fall back to non-cancelled/returned order value so dashboard KPI is still meaningful.
        var operationalRevenueFiltered = filteredOrders
            .Where(o => o.Status != OrderStatus.Cancelled
                && o.Status != OrderStatus.Returned)
            .Sum(o => o.TotalAmount);

        var deliveredRevenue = paidRevenueFiltered > 0
            ? paidRevenueFiltered
            : operationalRevenueFiltered;

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

        var recentOrders = filteredOrders
            .Take(6)
            .Select(o => new RecentOrderViewModel
            {
                OrderId = o.Id,
                CustomerName = string.IsNullOrWhiteSpace(o.UserFullName) ? "Khách hàng" : o.UserFullName,
                OrderDate = o.OrderDate.ToLocalTime(),
                StatusLabel = o.Status.ToString(),
                TotalAmount = o.TotalAmount
            })
            .ToList();

        var topSellingProducts = filteredOrders
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

        static string BuildTrendLabel(IReadOnlyList<decimal> values)
        {
            if (values.Count < 2)
            {
                return "Chưa đủ dữ liệu";
            }

            var first = values.First();
            if (first == 0)
            {
                return values.Last() == 0 ? "Không biến động" : "Tăng";
            }

            var deltaPercent = (values.Last() - first) / first;
            var avg = values.Average();
            var variance = values.Sum(v => (v - avg) * (v - avg)) / values.Count;
            var stdDev = (decimal)Math.Sqrt((double)variance);
            var volatilityRatio = avg <= 0 ? 0 : stdDev / avg;

            if (Math.Abs(deltaPercent) < 0.01m && volatilityRatio < 0.03m)
            {
                return "Ít biến động";
            }

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

            return "Ổn định ngắn hạn";
        }

        var chartMetas = new[]
        {
            new { Title = "Doanh thu dự báo", Dataset = "Doanh thu", YAxis = "Đồng", Unit = "đ" },
            new { Title = "Số đơn hàng dự báo", Dataset = "Đơn hàng", YAxis = "Đơn hàng", Unit = "đơn" },
            new { Title = "Sản phẩm bán ra dự báo", Dataset = "Sản phẩm", YAxis = "Sản phẩm", Unit = "sản phẩm" }
        };

        var nowLocalDate = DateTime.Now.Date;
        var historyStart = nowLocalDate.AddDays(-56);
        var validOrders = filteredOrders
            .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned)
            .ToList();

        var orderCountByDate = validOrders
            .GroupBy(o => o.OrderDate.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => (decimal)g.Count());

        var productCountByDate = validOrders
            .GroupBy(o => o.OrderDate.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => (decimal)g.Sum(x => x.Items.Sum(i => i.Quantity)));

        var revenueByDate = validOrders
            .GroupBy(o => o.OrderDate.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

        var dailyDates = Enumerable
            .Range(0, (nowLocalDate - historyStart).Days + 1)
            .Select(offset => historyStart.AddDays(offset))
            .ToList();

        static List<decimal> BuildSeries(IReadOnlyList<DateTime> dates, IReadOnlyDictionary<DateTime, decimal> valuesByDate)
        {
            return dates
                .Select(d => valuesByDate.TryGetValue(d, out var value) ? value : 0m)
                .ToList();
        }

        static List<decimal> ForecastDailySeries(IReadOnlyList<DateTime> historyDates, IReadOnlyList<decimal> historyValues, int horizonDays)
        {
            if (historyDates.Count == 0 || historyValues.Count == 0 || historyDates.Count != historyValues.Count)
            {
                return Enumerable.Repeat(0m, horizonDays).ToList();
            }

            var last7 = historyValues.TakeLast(Math.Min(7, historyValues.Count)).ToList();
            var prev7 = historyValues.Skip(Math.Max(0, historyValues.Count - 14)).Take(Math.Min(7, historyValues.Count)).ToList();

            var shortAvg = last7.Any() ? last7.Average() : 0m;
            var prevAvg = prev7.Any() ? prev7.Average() : shortAvg;
            var trend = prevAvg <= 0 ? 0m : (shortAvg - prevAvg) / prevAvg;

            var weekdayAverages = historyDates
                .Zip(historyValues, (date, value) => new { date.DayOfWeek, Value = value })
                .GroupBy(x => x.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Average(x => x.Value));

            var lastDate = historyDates.Last();
            var forecast = new List<decimal>(horizonDays);
            for (var i = 1; i <= horizonDays; i++)
            {
                var targetDate = lastDate.AddDays(i);
                var weekdayBase = weekdayAverages.TryGetValue(targetDate.DayOfWeek, out var avg)
                    ? avg
                    : shortAvg;

                var blendedBase = (weekdayBase * 0.7m) + (shortAvg * 0.3m);
                var trendFactor = 1m + (trend * (i / 7m) * 0.45m);
                var value = Math.Max(0m, blendedBase * trendFactor);
                forecast.Add(value);
            }

            return forecast;
        }

        static List<decimal> AggregateWeekly(IReadOnlyList<decimal> dailyValues, int weeks)
        {
            var result = new List<decimal>(weeks);
            for (var i = 0; i < weeks; i++)
            {
                var weekSum = dailyValues.Skip(i * 7).Take(7).Sum();
                result.Add(Math.Round(weekSum, 2));
            }

            return result;
        }

        var orderHistory = BuildSeries(dailyDates, orderCountByDate);
        var productHistory = BuildSeries(dailyDates, productCountByDate);
        var revenueHistory = BuildSeries(dailyDates, revenueByDate);

        var forecastWeeks = 4;
        var forecastDays = forecastWeeks * 7;

        var orderForecastDaily = ForecastDailySeries(dailyDates, orderHistory, forecastDays);
        var productForecastDaily = ForecastDailySeries(dailyDates, productHistory, forecastDays);
        var revenueForecastDaily = ForecastDailySeries(dailyDates, revenueHistory, forecastDays);

        var weekStart = nowLocalDate;
        var weeklyLabels = Enumerable.Range(1, forecastWeeks)
            .Select(i => weekStart.AddDays(i * 7).ToString("dd/MM", CultureInfo.InvariantCulture))
            .ToList();

        var miniCharts = new List<ForecastMiniChartViewModel>
        {
            new()
            {
                ProductId = 0,
                ProductName = "Toan he thong",
                ChartTitle = chartMetas[0].Title,
                DatasetLabel = chartMetas[0].Dataset,
                YAxisLabel = chartMetas[0].YAxis,
                UnitLabel = chartMetas[0].Unit,
                Labels = weeklyLabels,
                Values = AggregateWeekly(revenueForecastDaily, forecastWeeks)
            },
            new()
            {
                ProductId = 0,
                ProductName = "Toan he thong",
                ChartTitle = chartMetas[1].Title,
                DatasetLabel = chartMetas[1].Dataset,
                YAxisLabel = chartMetas[1].YAxis,
                UnitLabel = chartMetas[1].Unit,
                Labels = weeklyLabels,
                Values = AggregateWeekly(orderForecastDaily, forecastWeeks)
            },
            new()
            {
                ProductId = 0,
                ProductName = "Toan he thong",
                ChartTitle = chartMetas[2].Title,
                DatasetLabel = chartMetas[2].Dataset,
                YAxisLabel = chartMetas[2].YAxis,
                UnitLabel = chartMetas[2].Unit,
                Labels = weeklyLabels,
                Values = AggregateWeekly(productForecastDaily, forecastWeeks)
            }
        };

        foreach (var chart in miniCharts)
        {
            chart.TrendLabel = BuildTrendLabel(chart.Values);
        }

        // If AI model already has a richer product-specific forecast, append up to one chart as optional reference.
        var highlightedProductId = topSellingProducts.FirstOrDefault()?.ProductId;
        if (highlightedProductId.HasValue)
        {
            try
            {
                var forecastData = (await _forecastService.GetForecastForProductAsync(highlightedProductId.Value, 4))
                    .Where(x => !x.IsHistorical)
                    .OrderBy(x => x.TargetDate)
                    .ToList();

                if (forecastData.Any())
                {
                    var aiValues = forecastData.Select(x => Math.Round(x.PredictedDemand, 2)).ToList();
                    miniCharts[2].Values = aiValues;
                    miniCharts[2].Labels = forecastData.Select(x => x.TargetDate.ToLocalTime().ToString("dd/MM")).ToList();
                    miniCharts[2].TrendLabel = BuildTrendLabel(miniCharts[2].Values);
                    miniCharts[2].ProductName = forecastData.First().ProductName;
                }
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

        model.Filter = new DashboardFilterViewModel
        {
            Preset = normalizedPreset,
            FromDate = fromDate,
            ToDate = toDate,
            OrderStatus = filterOrderStatus?.ToString(),
            PaymentState = normalizedPaymentState,
            AppliedRangeLabel = startLocal.HasValue || endLocal.HasValue
                ? $"{(startLocal.HasValue ? startLocal.Value.ToString("dd/MM/yyyy") : "Từ đầu")} - {(endLocal.HasValue ? endLocal.Value.ToString("dd/MM/yyyy") : "Hiện tại")}"
                : "Toàn thời gian"
        };

        var now = DateTime.UtcNow;
        DateTime weekStartUtc = GetWeekStart(now, DayOfWeek.Monday);
        var monthStartUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var quarterStartMonth = ((now.Month - 1) / 3) * 3 + 1;
        var quarterStartUtc = new DateTime(now.Year, quarterStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStartUtc = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        model.PeriodMetrics = new List<PeriodMetricViewModel>
        {
            BuildPeriodMetric("year", "Năm", yearStartUtc, now),
            BuildPeriodMetric("quarter", "Quý", quarterStartUtc, now),
            BuildPeriodMetric("month", "Tháng", monthStartUtc, now),
            BuildPeriodMetric("week", "Tuần", weekStartUtc, now)
        };

        PeriodMetricViewModel BuildPeriodMetric(string key, string title, DateTime fromUtc, DateTime toUtc)
        {
            var periodOrders = filteredOrders
                .Where(o => o.OrderDate >= fromUtc && o.OrderDate <= toUtc)
                .ToList();

            var paidRevenue = periodOrders
                .Where(o => o.IsPaid || string.Equals(o.PaymentStatus, PaymentStatus.Paid.ToString(), StringComparison.OrdinalIgnoreCase))
                .Sum(o => o.TotalAmount);

            var fallbackRevenue = periodOrders
                .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned)
                .Sum(o => o.TotalAmount);

            return new PeriodMetricViewModel
            {
                Key = key,
                Title = title,
                OrderCount = periodOrders.Count,
                Revenue = paidRevenue > 0 ? paidRevenue : fallbackRevenue,
                RangeLabel = $"{fromUtc.ToLocalTime():dd/MM/yyyy} - {toUtc.ToLocalTime():dd/MM/yyyy}"
            };
        }

        if (model.LowStockCount > 0)
        {
            model.AdminAlerts.Add($"Cảnh báo: có {model.LowStockCount} sản phẩm đang ở mức tồn kho thấp.");
            foreach (var p in model.LowStockProducts.Take(3))
            {
                model.AdminAlerts.Add($"{p.ProductName}: tồn {p.StockQuantity}, mức tối thiểu {p.MinStockLevel}.");
            }
        }

        return View(model);
    }

    private static DateTime GetWeekStart(DateTime date, DayOfWeek startOfWeek)
    {
        var diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-diff);
    }
}
