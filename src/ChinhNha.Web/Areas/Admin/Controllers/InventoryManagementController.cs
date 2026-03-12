using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class InventoryManagementController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IProductService _productService;

    public InventoryManagementController(IInventoryService inventoryService, IProductService productService)
    {
        _inventoryService = inventoryService;
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllProductsAsync();
        return View(products);
    }

    public async Task<IActionResult> Transactions(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null) return NotFound();

        var transactions = await _inventoryService.GetProductTransactionsAsync(productId);

        ViewBag.Product = product;
        return View(transactions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordStock(int productId, TransactionType type, int quantity, string? note)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (quantity <= 0)
        {
            TempData["ErrorMessage"] = "Số lượng phải lớn hơn 0.";
            return RedirectToAction(nameof(Transactions), new { productId });
        }

        try
        {
            await _inventoryService.RecordTransactionAsync(
                productId: productId,
                type: type,
                quantity: quantity,
                note: note,
                createdByUserId: userId
            );
            TempData["SuccessMessage"] = "Cập nhật tồn kho thành công!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
        }

        return RedirectToAction(nameof(Transactions), new { productId });
    }
}
