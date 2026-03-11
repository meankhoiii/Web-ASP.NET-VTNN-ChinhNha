namespace ChinhNha.Application.DTOs.Orders;

public class CartDto
{
    public int Id { get; set; }
    public string? UserId { get; set; } // Null for guest cart
    public string SessionId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public List<CartItemDto> Items { get; set; } = new();
    
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
}
