using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetUserOrdersWithDetailsAsync(string userId);
    Task<Order?> GetOrderWithDetailsByIdAsync(int id);
    Task<IEnumerable<Order>> GetAllOrdersWithDetailsAsync();
}
