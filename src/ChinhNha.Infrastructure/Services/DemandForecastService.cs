using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Enums;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace ChinhNha.Infrastructure.Services;

// Internal data schemas matching the training schemas
internal class DemandInput
{
    public float WeeklyDemand { get; set; }
}

internal class DemandPrediction
{
    public float[] ForecastedValues { get; set; } = Array.Empty<float>();
    public float[] LowerBound { get; set; } = Array.Empty<float>();
    public float[] UpperBound { get; set; } = Array.Empty<float>();
}

/// <summary>
/// Loads pre-trained ML.NET SSA models from .zip files to generate
/// weekly demand forecasts per product.
/// Registered as Singleton since MLContext and model loading is expensive.
/// </summary>
public class DemandForecastService : IInventoryForecastService
{
    private readonly string _modelsPath;
    private readonly MLContext _mlContext;
    private readonly IServiceProvider _serviceProvider;

    public DemandForecastService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _mlContext = new MLContext(seed: 42);
        _modelsPath = Path.Combine(AppContext.BaseDirectory, "MLModels");
    }

    public async Task<IEnumerable<InventoryForecastDto>> GetForecastForProductAsync(int productId, int weeksAhead = 4)
    {
        var modelPath = Path.Combine(_modelsPath, $"demand_forecast_product_{productId}.zip");

        if (!File.Exists(modelPath))
            return Array.Empty<InventoryForecastDto>();

        try
        {
            ITransformer loadedModel = _mlContext.Model.Load(modelPath, out _);
            var forecastEngine = loadedModel.CreateTimeSeriesEngine<DemandInput, DemandPrediction>(_mlContext);
            var prediction = forecastEngine.Predict();
            var now = DateTime.UtcNow;
            var currentWeekStart = GetWeekStart(now, DayOfWeek.Monday);
            var results = new List<InventoryForecastDto>();

            int count = Math.Min(prediction.ForecastedValues.Length, weeksAhead);
            for (int i = 0; i < count; i++)
            {
                var targetDate = currentWeekStart.AddDays((i + 1) * 7);
                results.Add(new InventoryForecastDto
                {
                    ProductId = productId,
                    ForecastDate = now,
                    TargetDate = targetDate,
                    PredictedDemand = (decimal)Math.Max(0, prediction.ForecastedValues[i]),
                    ConfidenceLower = (decimal)Math.Max(0, prediction.LowerBound[i]),
                    ConfidenceUpper = (decimal)prediction.UpperBound[i],
                    IsHistorical = false
                });
            }

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var targetDates = results.Select(r => r.TargetDate).ToList();
            var oldRows = await db.InventoryForecasts
                .Where(f => f.ProductId == productId && targetDates.Contains(f.ForecastDate))
                .ToListAsync();

            if (oldRows.Any())
            {
                db.InventoryForecasts.RemoveRange(oldRows);
            }

            foreach (var dto in results)
            {
                db.InventoryForecasts.Add(new InventoryForecast
                {
                    ProductId = productId,
                    ForecastDate = dto.TargetDate,
                    PredictedDemand = dto.PredictedDemand,
                    ConfidenceLower = dto.ConfidenceLower,
                    ConfidenceUpper = dto.ConfidenceUpper,
                    ModelVersion = "SSA_v1",
                    GeneratedAt = DateTime.Now
                });
            }

            await db.SaveChangesAsync();
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DemandForecastService] Failed to load model for product {productId}: {ex.Message}");
            return Array.Empty<InventoryForecastDto>();
        }
    }

    public async Task TrainModelsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var products = await context.Products.ToListAsync();

        if (!Directory.Exists(_modelsPath))
        {
            Directory.CreateDirectory(_modelsPath);
        }

        foreach (var product in products)
        {
            var transactions = await context.InventoryTransactions
                .Where(t => t.ProductId == product.Id && t.TransactionType == TransactionType.Export)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new { t.CreatedAt, t.Quantity })
                .ToListAsync();

            var weeklyData = transactions
                .GroupBy(t => GetWeekStart(t.CreatedAt, DayOfWeek.Monday))
                .OrderBy(g => g.Key)
                .Select(g => new DemandInput
                {
                    WeeklyDemand = (float)g.Sum(x => x.Quantity)
                })
                .ToList();

            if (weeklyData.Count < 12)
                continue;

            var dataView = _mlContext.Data.LoadFromEnumerable(weeklyData);

            int horizon = 4;
            int windowSize = Math.Min(8, Math.Max(2, weeklyData.Count / 2));

            var pipeline = _mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(DemandPrediction.ForecastedValues),
                inputColumnName: nameof(DemandInput.WeeklyDemand),
                windowSize: windowSize,
                seriesLength: weeklyData.Count,
                trainSize: weeklyData.Count,
                horizon: horizon,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: nameof(DemandPrediction.LowerBound),
                confidenceUpperBoundColumn: nameof(DemandPrediction.UpperBound)
            );

            var model = pipeline.Fit(dataView);
            var modelPath = Path.Combine(_modelsPath, $"demand_forecast_product_{product.Id}.zip");

            var forecastEngine = model.CreateTimeSeriesEngine<DemandInput, DemandPrediction>(_mlContext);
            forecastEngine.CheckPoint(_mlContext, modelPath);
        }
    }

    public async Task UpdateActualDemandAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingForecasts = await db.InventoryForecasts
            .Where(f => f.ActualDemand == null && f.ForecastDate <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var forecast in pendingForecasts)
        {
            var weekStart = forecast.ForecastDate.AddDays(-7);
            var actual = await db.InventoryTransactions
                .Where(t => t.ProductId == forecast.ProductId
                    && t.TransactionType == TransactionType.Export
                    && t.CreatedAt >= weekStart
                    && t.CreatedAt < forecast.ForecastDate)
                .SumAsync(t => (int?)t.Quantity) ?? 0;

            forecast.ActualDemand = actual;
            if (actual > 0)
            {
                forecast.MAPE = Math.Abs((actual - forecast.PredictedDemand) / actual) * 100m;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task<ReorderAnalysisDto?> CalculateReorderPointAsync(int productId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = await db.Products
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return null;
        }

        int leadTimeDays = product.Supplier?.LeadTimeDays ?? 7;

        var exports = await db.InventoryTransactions
            .Where(t => t.ProductId == productId
                && t.TransactionType == TransactionType.Export
                && t.CreatedAt >= DateTime.UtcNow.AddDays(-90))
            .Select(t => new { t.CreatedAt, t.Quantity })
            .ToListAsync();

        if (!exports.Any())
        {
            return null;
        }

        var dailySales = exports
            .GroupBy(e => e.CreatedAt.Date)
            .Select(g => (decimal)g.Sum(x => x.Quantity))
            .ToList();

        decimal avgDailySales = dailySales.Average();
        decimal maxDailySales = dailySales.Max();
        decimal safetyStock = Math.Max(0, (maxDailySales - avgDailySales) * leadTimeDays);
        decimal reorderPoint = (avgDailySales * leadTimeDays) + safetyStock;
        decimal suggestedOrderQty = Math.Max(0, reorderPoint - product.StockQuantity);

        return new ReorderAnalysisDto
        {
            ProductId = productId,
            AvgDailySales = avgDailySales,
            MaxDailySales = maxDailySales,
            SafetyStock = safetyStock,
            ReorderPoint = reorderPoint,
            CurrentStock = product.StockQuantity,
            NeedsReorder = product.StockQuantity <= reorderPoint,
            SuggestedOrderQty = suggestedOrderQty,
            LeadTimeDays = leadTimeDays
        };
    }

    public async Task<PurchaseOrder?> CreateAISuggestedPOAsync(int productId)
    {
        var analysis = await CalculateReorderPointAsync(productId);
        if (analysis == null || !analysis.NeedsReorder || analysis.SuggestedOrderQty <= 0)
        {
            return null;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = await db.Products
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null || !product.SupplierId.HasValue)
        {
            return null;
        }

        var quantity = (int)Math.Ceiling(analysis.SuggestedOrderQty);
        if (quantity <= 0)
        {
            return null;
        }

        var poCode = await GenerateUniquePoCodeAsync(db, productId);
        var unitCost = product.BasePrice;

        var po = new PurchaseOrder
        {
            POCode = poCode,
            SupplierId = product.SupplierId.Value,
            Status = PurchaseOrderStatus.Draft,
            IsAISuggested = true,
            OrderDate = DateTime.Now,
            ExpectedDeliveryDate = DateTime.Now.AddDays(product.Supplier?.LeadTimeDays ?? 7),
            Note = $"AI Forecast gợi ý. Tồn kho: {analysis.CurrentStock}, Reorder Point: {analysis.ReorderPoint:F0}, Safety Stock: {analysis.SafetyStock:F0}",
            CreatedAt = DateTime.Now,
            Items = new List<PurchaseOrderItem>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitCost = unitCost,
                    TotalCost = unitCost * quantity
                }
            }
        };

        po.TotalAmount = po.Items.Sum(i => i.TotalCost);

        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();
        return po;
    }

    private static DateTime GetWeekStart(DateTime date, DayOfWeek startOfWeek)
    {
        var diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-diff);
    }

    private static async Task<string> GenerateUniquePoCodeAsync(AppDbContext db, int productId)
    {
        var baseCode = $"AI-{DateTime.Now:yyyyMMdd}-{productId}";
        var code = baseCode;
        int suffix = 1;

        while (await db.PurchaseOrders.AnyAsync(p => p.POCode == code))
        {
            code = $"{baseCode}-{suffix}";
            suffix++;
        }

        return code;
    }
}
