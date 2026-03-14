using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Domain.Entities;

namespace ChinhNha.Application.Interfaces;

/// <summary>
/// Interface for the AI-powered demand forecasting service.
/// Implementation lives in Infrastructure (ML.NET), following Clean Architecture.
/// </summary>
public interface IInventoryForecastService
{
    /// <summary>
    /// Returns predicted demand for the next N weeks for a given product.
    /// </summary>
    Task<IEnumerable<InventoryForecastDto>> GetForecastForProductAsync(int productId, int weeksAhead = 4);
    
    /// <summary>
    /// Trains and saves forecast models for products using historical data.
    /// </summary>
    Task TrainModelsAsync();

    /// <summary>
    /// Backfills actual demand and computes MAPE for closed forecast windows.
    /// </summary>
    Task UpdateActualDemandAsync();

    /// <summary>
    /// Computes inventory reorder analysis from recent transaction history.
    /// </summary>
    Task<ReorderAnalysisDto?> CalculateReorderPointAsync(int productId);

    /// <summary>
    /// Creates a draft AI-suggested purchase order when product needs reorder.
    /// </summary>
    Task<PurchaseOrder?> CreateAISuggestedPOAsync(int productId);
}
