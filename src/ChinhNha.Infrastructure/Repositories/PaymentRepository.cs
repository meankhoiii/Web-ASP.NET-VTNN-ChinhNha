using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChinhNha.Infrastructure.Repositories;

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId)
    {
        return await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<IReadOnlyDictionary<int, Payment>> GetByOrderIdsAsync(IEnumerable<int> orderIds)
    {
        var ids = orderIds.Distinct().ToList();
        return await _dbContext.Payments
            .Where(p => ids.Contains(p.OrderId))
            .ToDictionaryAsync(p => p.OrderId);
    }
}