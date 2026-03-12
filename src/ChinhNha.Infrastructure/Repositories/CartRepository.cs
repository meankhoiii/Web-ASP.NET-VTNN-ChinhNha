using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChinhNha.Infrastructure.Repositories;

public class CartRepository : GenericRepository<Cart>, ICartRepository
{
    public CartRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Cart?> GetCartWithItemsByIdAsync(int cartId)
    {
        return await _dbContext.Carts
            .Include(c => c.CartItems)
            .ThenInclude(i => i.Product)
            .Include(c => c.CartItems)
            .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(c => c.Id == cartId);
    }

    public async Task<Cart?> GetCartWithItemsAsync(string? userId, string sessionId)
    {
        IQueryable<Cart> query = _dbContext.Carts
            .Include(c => c.CartItems)
            .ThenInclude(i => i.Product)
            .Include(c => c.CartItems)
            .ThenInclude(i => i.ProductVariant);

        if (!string.IsNullOrEmpty(userId))
            return await query.FirstOrDefaultAsync(c => c.UserId == userId);

        return await query.FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }

    public async Task<Cart?> GetCartBySessionIdAsync(string sessionId)
    {
        return await _dbContext.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }
}
