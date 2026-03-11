using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Application.Interfaces;
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

    public DemandForecastService()
    {
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
            // Load the saved ITransformer from file
            ITransformer loadedModel = _mlContext.Model.Load(modelPath, out _);

            // Create the time series engine from the loaded transformer
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
}
