namespace ChinhNha.Application.DTOs.Customers;

public class CustomerProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
}
