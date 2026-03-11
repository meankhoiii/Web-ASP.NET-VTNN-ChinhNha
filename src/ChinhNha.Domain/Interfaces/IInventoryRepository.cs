using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

public interface IInventoryRepository : IRepository<InventoryTransaction>
{
    Task<IEnumerable<InventoryTransaction>> GetTransactionsByProductIdAsync(int productId);
    Task<int> GetCurrentStockQuantityAsync(int productId, int? variantId = null);
}
