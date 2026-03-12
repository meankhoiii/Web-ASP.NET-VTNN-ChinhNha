using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChinhNha.Infrastructure.Services;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class InventoryManagementController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IProductService _productService;
    private readonly IInventoryImportExportService _importExportService;

    public InventoryManagementController(
        IInventoryService inventoryService, 
        IProductService productService,
        IInventoryImportExportService importExportService)
    {
        _inventoryService = inventoryService;
        _productService = productService;
        _importExportService = importExportService;
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

    /// <summary>
    /// Export current inventory to Excel file
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportInventory()
    {
        try
        {
            var products = await _productService.GetAllProductsAsync();
            var transactions = new List<ChinhNha.Application.DTOs.Inventory.InventoryTransactionDto>();

            // Get all transactions for all products
            foreach (var product in products)
            {
                var productTransactions = await _inventoryService.GetProductTransactionsAsync(product.Id);
                transactions.AddRange(productTransactions);
            }

            var fileContent = await _importExportService.ExportToExcelAsync(transactions);
            string fileName = $"InventoryExport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(fileContent, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi xuất file: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Display import template
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DownloadImportTemplate()
    {
        try
        {
            var templateTransactions = new List<ChinhNha.Application.DTOs.Inventory.InventoryTransactionDto>
            {
                new()
                {
                    ProductName = "Phân bón NPK 16-16-16",
                    Type = TransactionType.Import,
                    Quantity = 100,
                    UnitCost = 50000,
                    Note = "[Ví dụ] Cập nhật lô hàng mới"
                }
            };

            var fileContent = await _importExportService.ExportToExcelAsync(templateTransactions);
            string fileName = $"ImportTemplate_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(fileContent, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi tải template: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Handle bulk import from Excel file
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportInventory(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file để nhập.";
            return RedirectToAction(nameof(Index));
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Chỉ hỗ trợ file Excel (.xlsx)";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using (var stream = file.OpenReadStream())
            {
                // Parse Excel file
                var transactions = await _importExportService.ImportFromExcelAsync(stream);

                if (!transactions.Any())
                {
                    TempData["ErrorMessage"] = "File không chứa dữ liệu hợp lệ.";
                    return RedirectToAction(nameof(Index));
                }

                // Bulk import to database
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var (successCount, failureCount, errors) = await _importExportService
                    .BulkImportTransactionsAsync(transactions, userId);

                if (successCount > 0)
                {
                    TempData["SuccessMessage"] = $"✓ Nhập thành công {successCount} giao dịch";
                }

                if (errors.Any())
                {
                    TempData["WarningMessage"] = $"⚠ {failureCount} lỗi:\n" + 
                        string.Join("\n", errors.Take(5)) +
                        (errors.Count > 5 ? $"\n... và {errors.Count - 5} lỗi khác" : "");
                }
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi xử lý file: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
