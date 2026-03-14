using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Enums;
using ChinhNha.Infrastructure.Data;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class InventoryController : Controller
{
    private readonly IProductService _productService;
    private readonly IInventoryForecastService _forecastService;
    private readonly AppDbContext _dbContext;

    public InventoryController(IProductService productService, IInventoryForecastService forecastService, AppDbContext dbContext)
    {
        _productService = productService;
        _forecastService = forecastService;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(string filter = "need")
    {
        var allProducts = await _productService.GetAllProductsAsync();

        var alertItems = new List<InventoryAlertItemViewModel>();
        foreach (var product in allProducts)
        {
            var analysis = await _forecastService.CalculateReorderPointAsync(product.Id);
            var reorderPoint = analysis?.ReorderPoint ?? product.MinStockLevel;
            var urgentGap = reorderPoint - product.StockQuantity;

            var item = new InventoryAlertItemViewModel
            {
                ProductId = product.Id,
                ProductCode = product.SKU ?? $"SP-{product.Id}",
                ProductName = product.Name,
                CurrentStock = product.StockQuantity,
                MinStockLevel = product.MinStockLevel,
                ReorderPoint = Math.Round(reorderPoint, 0),
                UrgencyScore = urgentGap
            };

            if (product.StockQuantity <= reorderPoint)
            {
                item.StatusCode = "need";
                item.StatusLabel = "Cần nhập";
            }
            else if (product.StockQuantity <= reorderPoint * 1.5m)
            {
                item.StatusCode = "warning";
                item.StatusLabel = "Sắp hết";
            }
            else
            {
                item.StatusCode = "ok";
                item.StatusLabel = "Đủ hàng";
            }

            alertItems.Add(item);
        }

        var normalizedFilter = (filter ?? "need").Trim().ToLowerInvariant();
        IEnumerable<InventoryAlertItemViewModel> filteredItems = alertItems;
        if (normalizedFilter == "need")
        {
            filteredItems = filteredItems.Where(x => x.StatusCode == "need");
        }
        else if (normalizedFilter == "warning")
        {
            filteredItems = filteredItems.Where(x => x.StatusCode == "warning");
        }
        else if (normalizedFilter == "ok")
        {
            filteredItems = filteredItems.Where(x => x.StatusCode == "ok");
        }

        var model = new InventoryForecastViewModel
        {
            Products = allProducts,
            AlertFilter = normalizedFilter,
            AlertItems = filteredItems
                .OrderBy(x => x.StatusCode == "need" ? 0 : (x.StatusCode == "warning" ? 1 : 2))
                .ThenByDescending(x => x.UrgencyScore)
                .ThenBy(x => x.ProductName)
                .ToList()
        };

        return View("Overview", model);
    }

    public async Task<IActionResult> Detail(int productId)
    {
        var allProducts = await _productService.GetAllProductsAsync();

        var model = new InventoryForecastViewModel
        {
            Products = allProducts,
            SelectedProductId = productId
        };

        model.SelectedProduct = allProducts.FirstOrDefault(p => p.Id == productId);
        if (model.SelectedProduct == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để dự báo.";
            return RedirectToAction(nameof(Index));
        }

        var rawForecasts = (await _forecastService.GetForecastForProductAsync(productId, 4)).ToList();

        var sinceDate = DateTime.UtcNow.AddMonths(-6);
        var exportTransactions = await _dbContext.InventoryTransactions
            .Where(t => t.ProductId == productId
                        && t.TransactionType == TransactionType.Export
                        && t.CreatedAt >= sinceDate)
            .Select(t => new { t.CreatedAt, t.Quantity })
            .ToListAsync();

        var actualWeekly = exportTransactions
            .GroupBy(t => GetWeekStart(t.CreatedAt, DayOfWeek.Monday))
            .Select(g => new WeeklyDemandPointViewModel
            {
                WeekStart = g.Key,
                Quantity = g.Sum(x => x.Quantity)
            })
            .OrderBy(x => x.WeekStart)
            .ToList();

        model.ActualWeeklyDemands = actualWeekly;
        model.ReorderAnalysis = await _forecastService.CalculateReorderPointAsync(productId);
        model.LatestMape = await _dbContext.InventoryForecasts
            .Where(f => f.ProductId == productId && f.MAPE != null)
            .OrderByDescending(f => f.GeneratedAt)
            .Select(f => f.MAPE)
            .FirstOrDefaultAsync();

        model.AvgForecastDemand = rawForecasts.Any()
            ? Math.Round(rawForecasts.Average(x => x.PredictedDemand), 2)
            : 0;

        var firstForecast = rawForecasts.OrderBy(x => x.TargetDate).FirstOrDefault();
        if (firstForecast != null)
        {
            model.ConfidenceLower = firstForecast.ConfidenceLower;
            model.ConfidenceUpper = firstForecast.ConfidenceUpper;

            if (firstForecast.PredictedDemand > 0 && firstForecast.ConfidenceLower.HasValue && firstForecast.ConfidenceUpper.HasValue)
            {
                var width = firstForecast.ConfidenceUpper.Value - firstForecast.ConfidenceLower.Value;
                model.ConfidenceWidthPercent = Math.Round((width / firstForecast.PredictedDemand) * 100m, 2);
            }
        }

        var historicalRows = actualWeekly.Select(x => new ChinhNha.Application.DTOs.Inventory.InventoryForecastDto
        {
            ProductId = productId,
            ForecastDate = DateTime.UtcNow,
            TargetDate = x.WeekStart,
            PredictedDemand = 0,
            ActualDemand = x.Quantity,
            IsHistorical = true
        });

        model.Forecasts = historicalRows
            .Concat(rawForecasts)
            .OrderBy(x => x.TargetDate)
            .ToList();

        var modelPath = Path.Combine(
            AppContext.BaseDirectory,
            "MLModels",
            $"demand_forecast_product_{productId}.zip");

        if (System.IO.File.Exists(modelPath))
        {
            model.LastModelUpdatedAt = System.IO.File.GetLastWriteTime(modelPath);
        }

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrainAi(int? productId)
    {
        try
        {
            await _forecastService.TrainModelsAsync();
            await _forecastService.UpdateActualDemandAsync();
            TempData["SuccessMessage"] = "Đã cập nhật dữ liệu AI và đồng bộ sai số dự báo (MAPE) thành công.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Lỗi khi cập nhật mô hình: " + ex.Message;
        }

        if (productId.HasValue)
        {
            return RedirectToAction(nameof(Detail), new { productId = productId.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSuggestedPo(int productId)
    {
        try
        {
            var po = await _forecastService.CreateAISuggestedPOAsync(productId);
            if (po == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm hiện chưa cần nhập thêm hoặc chưa đủ dữ liệu để tạo phiếu nhập gợi ý.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã tạo phiếu nhập kho gợi ý AI ở trạng thái Nháp: {po.POCode}.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Lỗi khi tạo phiếu nhập kho gợi ý AI: " + ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { productId });
    }

    private static DateTime GetWeekStart(DateTime date, DayOfWeek startOfWeek)
    {
        var diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}
