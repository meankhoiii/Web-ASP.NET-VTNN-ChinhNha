using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Application.Interfaces;
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
    private readonly IServiceScopeFactory _scopeFactory;

    public DemandForecastService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _mlContext = new MLContext(seed: 42);
        _modelsPath = Path.Combine(AppContext.BaseDirectory, "MLModels");
    }

    public Task<IEnumerable<InventoryForecastDto>> GetForecastForProductAsync(int productId, int weeksAhead = 4)
    {
        var modelPath = Path.Combine(_modelsPath, $"demand_forecast_product_{productId}.zip");

        if (!File.Exists(modelPath))
            return Task.FromResult<IEnumerable<InventoryForecastDto>>(Array.Empty<InventoryForecastDto>());

        try
        {
            ITransformer loadedModel = _mlContext.Model.Load(modelPath, out _);
            var forecastEngine = loadedModel.CreateTimeSeriesEngine<DemandInput, DemandPrediction>(_mlContext);
            var prediction = forecastEngine.Predict();
            var now = DateTime.UtcNow;
            var results = new List<InventoryForecastDto>();

            int count = Math.Min(prediction.ForecastedValues.Length, weeksAhead);
            for (int i = 0; i < count; i++)
            {
                results.Add(new InventoryForecastDto
                {
                    ProductId = productId,
                    ForecastDate = now,
                    TargetDate = now.AddDays((i + 1) * 7),
                    PredictedDemand = (decimal)Math.Max(0, prediction.ForecastedValues[i]),
                    ConfidenceLower = (decimal)Math.Max(0, prediction.LowerBound[i]),
                    ConfidenceUpper = (decimal)prediction.UpperBound[i],
                    IsHistorical = false
                });
            }

            return Task.FromResult<IEnumerable<InventoryForecastDto>>(results);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DemandForecastService] Failed to load model for product {productId}: {ex.Message}");
            return Task.FromResult<IEnumerable<InventoryForecastDto>>(Array.Empty<InventoryForecastDto>());
        }
    }

    public async Task TrainModelsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
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
                .Select(t => new DemandInput { WeeklyDemand = (float)t.Quantity })
                .ToListAsync();

            if (transactions.Count < 12)
                continue;

            var dataView = _mlContext.Data.LoadFromEnumerable(transactions);

            int horizon = 4;
            int windowSize = Math.Min(8, transactions.Count / 2);

            var pipeline = _mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(DemandPrediction.ForecastedValues),
                inputColumnName: nameof(DemandInput.WeeklyDemand),
                windowSize: windowSize,
                seriesLength: transactions.Count,
                trainSize: transactions.Count,
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
}
