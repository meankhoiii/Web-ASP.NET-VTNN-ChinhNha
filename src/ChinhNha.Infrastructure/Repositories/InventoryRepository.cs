using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChinhNha.Infrastructure.Repositories;

public class InventoryRepository : GenericRepository<InventoryTransaction>, IInventoryRepository
{
    public InventoryRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<int> GetCurrentStockQuantityAsync(int productId, int? variantId = null)
    {
        var query = _dbContext.InventoryTransactions
            .Where(t => t.ProductId == productId);

        if (variantId.HasValue)
            query = query.Where(t => t.ProductVariantId == variantId.Value);

        var lastTransaction = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync();

        return lastTransaction?.StockAfter ?? 0;
    }

    public async Task<IEnumerable<InventoryTransaction>> GetTransactionsByProductIdAsync(int productId)
    {
        return await _dbContext.InventoryTransactions
            .Include(t => t.Product)
            .Include(t => t.ProductVariant)
            .Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}
