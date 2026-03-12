using ChinhNha.Application.DTOs.Customers;
using ChinhNha.Application.DTOs.Orders;

namespace ChinhNha.Web.Models;

public class CustomerDashboardViewModel
{
    public CustomerProfileDto Profile { get; set; } = new();
    public List<OrderDto> RecentOrders { get; set; } = new();
}

public class CustomerOrdersViewModel
{
    public List<OrderDto> Orders { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalOrders { get; set; }
}
