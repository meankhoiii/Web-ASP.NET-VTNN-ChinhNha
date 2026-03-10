namespace ChinhNha.Domain.Entities;

public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
