using ChinhNha.Application.DTOs.Inventory;

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
}
