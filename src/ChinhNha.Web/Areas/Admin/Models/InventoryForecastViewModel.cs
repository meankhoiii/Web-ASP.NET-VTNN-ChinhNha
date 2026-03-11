using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Application.DTOs.Products;

namespace ChinhNha.Web.Areas.Admin.Models;

public class InventoryForecastViewModel
{
    public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
    public int? SelectedProductId { get; set; }
    
    public IEnumerable<InventoryForecastDto>? Forecasts { get; set; }
}
