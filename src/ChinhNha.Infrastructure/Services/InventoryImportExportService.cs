using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Enums;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using ChinhNha.Infrastructure.Data;

namespace ChinhNha.Infrastructure.Services;

public interface IInventoryImportExportService
{
    Task<List<InventoryTransactionDto>> ImportFromExcelAsync(Stream excelStream);
    Task<byte[]> ExportToExcelAsync(List<InventoryTransactionDto> transactions);
    Task<(int successCount, int failureCount, List<string> errors)> BulkImportTransactionsAsync(
        List<InventoryTransactionDto> transactions, 
        string userId);
}

public class InventoryImportExportService : IInventoryImportExportService
{
    private readonly AppDbContext _context;
    private readonly IInventoryService _inventoryService;

    public InventoryImportExportService(AppDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
        try { ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial; } catch { }
    }

    public async Task<List<InventoryTransactionDto>> ImportFromExcelAsync(Stream excelStream)
    {
        var list = new List<InventoryTransactionDto>();
        using (var pkg = new ExcelPackage(excelStream))
        {
            var ws = pkg.Workbook.Worksheets.FirstOrDefault();
            if (ws?.Dimension == null) return list;

            for (int r = 2; r <= ws.Dimension.Rows; r++)
            {
                try
                {
                    var prodName = ws.Cells[r, 1].Value?.ToString()?.Trim();
                    var typeStr = ws.Cells[r, 2].Value?.ToString()?.Trim();
                    var qtyStr = ws.Cells[r, 3].Value?.ToString()?.Trim();
                    var costStr = ws.Cells[r, 4].Value?.ToString()?.Trim();
                    var note = ws.Cells[r, 5].Value?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(prodName) || string.IsNullOrEmpty(typeStr)) continue;
                    if (!Enum.TryParse<TransactionType>(typeStr, true, out var tt)) continue;
                    if (!int.TryParse(qtyStr, out int qty) || qty <= 0) continue;

                    decimal cost = 0;
                    decimal.TryParse(costStr, out cost);

                    var prod = await _context.Products.FirstOrDefaultAsync(p => p.Name == prodName);
                    if (prod == null) continue;

                    list.Add(new InventoryTransactionDto
                    {
                        ProductId = prod.Id,
                        ProductName = prod.Name,
                        Type = tt,
                        Quantity = qty,
                        UnitCost = cost,
                        Note = note ?? ""
                    });
                }
                catch { continue; }
            }
        }
        return await Task.FromResult(list);
    }

    public async Task<byte[]> ExportToExcelAsync(List<InventoryTransactionDto> transactions)
    {
        using (var pkg = new ExcelPackage())
        {
            var ws = pkg.Workbook.Worksheets.Add("Inventory");
            
            ws.Cells[1, 1].Value = "Product";
            ws.Cells[1, 2].Value = "Type";
            ws.Cells[1, 3].Value = "Quantity";
            ws.Cells[1, 4].Value = "Before";
            ws.Cells[1, 5].Value = "After";
            ws.Cells[1, 6].Value = "Unit Cost";
            ws.Cells[1, 7].Value = "Notes";

            var header = ws.Cells[1, 1, 1, 7];
            header.Style.Font.Bold = true;
            header.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            int row = 2;
            foreach (var t in transactions)
            {
                ws.Cells[row, 1].Value = t.ProductName;
                ws.Cells[row, 2].Value = GetTypeVi(t.Type);
                ws.Cells[row, 3].Value = t.Quantity;
                ws.Cells[row, 4].Value = t.StockBefore;
                ws.Cells[row, 5].Value = t.StockAfter;
                ws.Cells[row, 6].Value = t.UnitCost;
                ws.Cells[row, 7].Value = t.Note;
                row++;
            }

            ws.Columns[1].Width = 20;
            ws.Columns[2].Width = 15;
            ws.Columns[3].Width = 12;
            ws.Columns[4].Width = 15;
            ws.Columns[5].Width = 15;
            ws.Columns[6].Width = 12;
            ws.Columns[7].Width = 25;

            return await Task.FromResult(pkg.GetAsByteArray());
        }
    }

    public async Task<(int successCount, int failureCount, List<string> errors)> BulkImportTransactionsAsync(
        List<InventoryTransactionDto> items, 
        string userId)
    {
        int ok = 0, bad = 0;
        var errs = new List<string>();

        foreach (var item in items)
        {
            try
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod == null)
                    throw new Exception($"Not found: {item.ProductName}");

                await _inventoryService.RecordTransactionAsync(
                    productId: item.ProductId,
                    type: item.Type,
                    quantity: item.Quantity,
                    note: item.Note,
                    createdByUserId: userId,
                    unitCost: item.UnitCost
                );
                ok++;
            }
            catch (Exception ex)
            {
                bad++;
                errs.Add($"{item.ProductName}: {ex.Message}");
            }
        }

        return await Task.FromResult((successCount: ok, failureCount: bad, errors: errs));
    }

    private string GetTypeVi(TransactionType type) => type switch
    {
        TransactionType.Import => "Nhập kho",
        TransactionType.Export => "Xuất kho",
        TransactionType.Return => "Trả lại",
        TransactionType.Adjustment => "Điều chỉnh",
        TransactionType.Loss => "Mất mát",
        _ => type.ToString()
    };
}
