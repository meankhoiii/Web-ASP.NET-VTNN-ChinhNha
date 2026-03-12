using ChinhNha.Application.DTOs.Customers;
using ChinhNha.Application.DTOs.Orders;

namespace ChinhNha.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerProfileDto?> GetCustomerProfileAsync(string userId);
    Task<bool> UpdateCustomerProfileAsync(string userId, string fullName, string phone, string? avatarUrl = null);
    
    Task<IEnumerable<OrderDto>> GetCustomerOrdersAsync(string userId, int pageNumber = 1, int pageSize = 10);
    Task<OrderDto?> GetCustomerOrderDetailAsync(string userId, int orderId);
    
    Task<int> GetTotalOrdersCountAsync(string userId);
    Task<decimal> GetTotalSpentAsync(string userId);
}
