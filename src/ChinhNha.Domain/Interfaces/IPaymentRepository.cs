using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByOrderIdAsync(int orderId);
    Task<IReadOnlyDictionary<int, Payment>> GetByOrderIdsAsync(IEnumerable<int> orderIds);
}