using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetCartWithItemsAsync(string? userId, string sessionId);
    Task<Cart?> GetCartWithItemsByIdAsync(int cartId);
    Task<Cart?> GetCartBySessionIdAsync(string sessionId);
}
