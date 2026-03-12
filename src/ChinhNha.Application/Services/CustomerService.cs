using AutoMapper;
using ChinhNha.Application.DTOs.Customers;
using ChinhNha.Application.DTOs.Orders;
using ChinhNha.Application.Interfaces;

namespace ChinhNha.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IAppUserService _userService;
    private readonly IOrderService _orderService;
    private readonly IMapper _mapper;

    public CustomerService(
        IAppUserService userService,
        IOrderService orderService,
        IMapper mapper)
    {
        _userService = userService;
        _orderService = orderService;
        _mapper = mapper;
    }

    public async Task<CustomerProfileDto?> GetCustomerProfileAsync(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return null;

        var totalOrders = await GetTotalOrdersCountAsync(userId);
        var totalSpent = await GetTotalSpentAsync(userId);

        var dto = new CustomerProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TotalOrders = totalOrders,
            TotalSpent = totalSpent
        };

        return dto;
    }

    public async Task<bool> UpdateCustomerProfileAsync(string userId, string fullName, string phone, string? avatarUrl = null)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return false;

        user.FullName = fullName ?? user.FullName;
        user.Phone = phone ?? user.Phone;
        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            user.AvatarUrl = avatarUrl;
        }

        return await _userService.UpdateUserAsync(user);
    }

    public async Task<IEnumerable<OrderDto>> GetCustomerOrdersAsync(string userId, int pageNumber = 1, int pageSize = 10)
    {
        var allOrders = await _orderService.GetUserOrdersAsync(userId);
        return allOrders
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<OrderDto?> GetCustomerOrderDetailAsync(string userId, int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.UserId != userId)
            return null;

        return order;
    }

    public async Task<int> GetTotalOrdersCountAsync(string userId)
    {
        var orders = await _orderService.GetUserOrdersAsync(userId);
        return orders.Count();
    }

    public async Task<decimal> GetTotalSpentAsync(string userId)
    {
        var orders = await _orderService.GetUserOrdersAsync(userId);
        return orders.Sum(o => o.TotalAmount);
    }
}
