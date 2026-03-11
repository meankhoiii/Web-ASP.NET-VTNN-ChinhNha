using ChinhNha.Domain.Enums;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using ChinhNha.AI.Training;

// --- Load configuration ---
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

// --- Setup DB Context ---
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString)
    .Options;

using var context = new AppDbContext(options);

// --- Step 1: Seed mock data ---
await MockDataGenerator.SeedMockTransactionsAsync(context);

// --- Step 2: Load weekly demand data per product ---
var products = await context.Products.Take(5).ToListAsync();

foreach (var product in products)
{
    Console.WriteLine($"\nTraining model for: {product.Name} (ID: {product.Id})");

    var transactions = await context.InventoryTransactions
        .Where(t => t.ProductId == product.Id && t.TransactionType == TransactionType.Export)
        .OrderBy(t => t.CreatedAt)
        .Select(t => new DemandRow { WeeklyDemand = (float)t.Quantity })
        .ToListAsync();

    if (transactions.Count < 12)
    {
        Console.WriteLine($"  Not enough data points ({transactions.Count}), skipping.");
        continue;
    }

    // --- Step 3: Train SSA Forecasting model ---
    var mlContext = new MLContext(seed: 42);
    var dataView = mlContext.Data.LoadFromEnumerable(transactions);

    int horizon = 4; // Forecast 4 weeks ahead
    int windowSize = 8;

    var pipeline = mlContext.Forecasting.ForecastBySsa(
        outputColumnName: nameof(DemandForecast.ForecastedValues),
        inputColumnName: nameof(DemandRow.WeeklyDemand),
        windowSize: windowSize,
        seriesLength: transactions.Count,
        trainSize: transactions.Count,
        horizon: horizon,
        confidenceLevel: 0.95f,
        confidenceLowerBoundColumn: nameof(DemandForecast.LowerBound),
        confidenceUpperBoundColumn: nameof(DemandForecast.UpperBound)
    );

    var model = pipeline.Fit(dataView);

    // --- Step 4: Save model ---
    var modelDir = Path.Combine(AppContext.BaseDirectory, "models");
    Directory.CreateDirectory(modelDir);
    var modelPath = Path.Combine(modelDir, $"demand_forecast_product_{product.Id}.zip");

    var forecastEngine = model.CreateTimeSeriesEngine<DemandRow, DemandForecast>(mlContext);
    forecastEngine.CheckPoint(mlContext, modelPath);
    Console.WriteLine($"  Model saved: {modelPath}");

    // --- Step 5: Test forecast ---
    var forecast = forecastEngine.Predict();
    Console.WriteLine($"  Next 4-week forecast: [{string.Join(", ", forecast.ForecastedValues.Select(v => v.ToString("F1")))}]");
    Console.WriteLine($"  Lower bound:          [{string.Join(", ", forecast.LowerBound.Select(v => v.ToString("F1")))}]");
    Console.WriteLine($"  Upper bound:          [{string.Join(", ", forecast.UpperBound.Select(v => v.ToString("F1")))}]");
}

Console.WriteLine("\n✅ Phase 4 training complete! Models saved to /models directory.");
