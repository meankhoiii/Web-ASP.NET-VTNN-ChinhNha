using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Domain.Enums;

namespace ChinhNha.Application.Interfaces;

public interface IInventoryService
{
    Task<IEnumerable<InventoryTransactionDto>> GetProductTransactionsAsync(int productId);
    Task<int> GetCurrentStockAsync(int productId, int? variantId = null);
    
    // Core Inventory Logic
    Task<InventoryTransactionDto> RecordTransactionAsync(
        int productId, 
        TransactionType type, 
        int quantity, 
        string? note = null,
        int? variantId = null,
        decimal? unitCost = null,
        int? orderId = null,
        int? purchaseOrderId = null,
        string? createdByUserId = null);

    Task<IEnumerable<InventoryForecastDto>> GetProductForecastsAsync(int productId);
}
