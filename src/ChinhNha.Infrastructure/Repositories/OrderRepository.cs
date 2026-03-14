using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Enums;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ChinhNha.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Order>> GetUserOrdersWithDetailsAsync(string userId)
    {
        return await _dbContext.Orders
                .AsNoTracking()
            .Include(o => o.User)
                .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderWithDetailsByIdAsync(int id)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetAllOrdersWithDetailsAsync(OrderStatus? status = null)
    {
        IQueryable<Order> query = _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderItems);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}
