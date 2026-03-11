using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Enums;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ChinhNha.AI.Training;

/// <summary>
/// Generates mock InventoryTransaction data with seasonal patterns
/// mimicking agricultural fertilizer demand cycles (peak before planting season).
/// </summary>
public static class MockDataGenerator
{
    private static readonly Random _rng = new(42); // Fixed seed for reproducibility

    /// <summary>
    /// Generates 2 years of daily or weekly export (sale) transactions per product,
    /// with a seasonal multiplier that mimics Vietnamese rice crop cycles:
    /// - Peak 1: Dec–Feb (winter-spring crop prep)
    /// - Peak 2: May–Jul (summer-autumn crop prep)
    /// </summary>
    public static async Task SeedMockTransactionsAsync(AppDbContext context)
    {
        // Auto-seed categories and products if empty
        await SeedProductsIfEmptyAsync(context);

        // Only seed transactions if none exist
        if (await context.InventoryTransactions.AnyAsync()) return;

        Console.WriteLine("Seeding mock inventory transactions...");

        var products = await context.Products.Take(5).ToListAsync();
        if (!products.Any())
        {
            Console.WriteLine("No products found. Please seed product data first.");
            return;
        }

        var startDate = DateTime.UtcNow.AddYears(-2);
        var transactions = new List<InventoryTransaction>();

        foreach (var product in products)
        {
            int currentStock = 200; // Initial stock

            // First, seed an initial import transaction
            transactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                TransactionType = TransactionType.Import,
                Quantity = currentStock,
                StockBefore = 0,
                StockAfter = currentStock,
                ReferenceType = "Manual",
                Note = "Tồn kho ban đầu (Mock)",
                CreatedAt = startDate.AddDays(-1)
            });

            // Generate weekly export data over 2 years (~104 data points)
            for (int week = 0; week < 104; week++)
            {
                var date = startDate.AddDays(week * 7);
                int month = date.Month;

                // Seasonal multiplier based on Vietnamese rice crop calendar
                double seasonalMultiplier = month switch
                {
                    12 or 1 or 2 => 2.5,  // Peak: Winter-spring crop prep
                    3 or 4 => 1.5,          // Moderate: Winter-spring crop applied
                    5 or 6 or 7 => 2.0,    // Peak: Summer-autumn crop prep
                    8 or 9 => 1.3,          // Moderate: Summer-autumn crop applied
                    10 or 11 => 0.8,        // Low: Resting season
                    _ => 1.0
                };

                int baseQuantity = _rng.Next(10, 30);
                int quantity = (int)(baseQuantity * seasonalMultiplier);
                quantity = Math.Max(1, quantity); // Min 1 unit

                // Replenish stock if running low
                if (currentStock < quantity * 2)
                {
                    int importQty = _rng.Next(100, 200);
                    transactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        TransactionType = TransactionType.Import,
                        Quantity = importQty,
                        StockBefore = currentStock,
                        StockAfter = currentStock + importQty,
                        ReferenceType = "PurchaseOrder",
                        Note = "Nhập kho định kỳ (Mock)",
                        CreatedAt = date.AddDays(-1)
                    });
                    currentStock += importQty;
                }

                // Record the sale (export) transaction
                transactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    TransactionType = TransactionType.Export,
                    Quantity = quantity,
                    StockBefore = currentStock,
                    StockAfter = currentStock - quantity,
                    ReferenceType = "Order",
                    Note = $"Xuất kho bán hàng tuần {week + 1} (Mock Data)",
                    CreatedAt = date
                });

                currentStock -= quantity;
            }

            // Update final product stock
            product.StockQuantity = currentStock;
        }

        await context.InventoryTransactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {transactions.Count} mock transactions for {products.Count} products.");
    }

    private static async Task SeedProductsIfEmptyAsync(AppDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        Console.WriteLine("No products found. Auto-seeding categories and products...");

        // Seed category first
        if (!await context.ProductCategories.AnyAsync())
        {
            var cat = new ProductCategory
            {
                Name = "Phân Bón",
                Slug = "phan-bon",
                Description = "Các loại phân bón nông nghiệp",
                CreatedAt = DateTime.UtcNow
            };
            await context.ProductCategories.AddAsync(cat);
            await context.SaveChangesAsync();
        }

        var category = await context.ProductCategories.FirstAsync();
        var products = new List<Product>
        {
            new() { Name = "Phân Urê 46%",       Slug = "phan-ure-46",         SKU = "URE-001", BasePrice = 350000, StockQuantity = 200, Unit = "Bao 50kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 20, CreatedAt = DateTime.UtcNow },
            new() { Name = "Phân NPK 16-16-8",   Slug = "phan-npk-16-16-8",   SKU = "NPK-001", BasePrice = 420000, StockQuantity = 150, Unit = "Bao 50kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 15, CreatedAt = DateTime.UtcNow },
            new() { Name = "Phân DAP 18-46-0",   Slug = "phan-dap-18-46-0",   SKU = "DAP-001", BasePrice = 520000, StockQuantity = 100, Unit = "Bao 50kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 10, CreatedAt = DateTime.UtcNow },
            new() { Name = "Phân Kali (KCl)",    Slug = "phan-kali-kcl",      SKU = "KAL-001", BasePrice = 480000, StockQuantity = 120, Unit = "Bao 50kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 10, CreatedAt = DateTime.UtcNow },
            new() { Name = "Phân Hữu Cơ Vi Sinh",Slug = "phan-huu-co-vi-sinh",SKU = "HUC-001", BasePrice = 280000, StockQuantity =  80, Unit = "Bao 25kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 10, CreatedAt = DateTime.UtcNow },
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {products.Count} products.");
    }
}
