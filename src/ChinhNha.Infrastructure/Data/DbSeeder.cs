using ChinhNha.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChinhNha.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // 1. Tạo Roles
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("Customer"))
        {
            await roleManager.CreateAsync(new IdentityRole("Customer"));
        }

        // 2. Tạo Admin User
        if (await userManager.FindByEmailAsync("admin@chinhnha.id.vn") == null)
        {
            var adminUser = new AppUser
            {
                UserName = "admin@chinhnha.id.vn",
                Email = "admin@chinhnha.id.vn",
                FullName = "Administrator",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 3. Tạo Category Mẫu (Nếu chưa có)
        if (!await context.ProductCategories.AnyAsync())
        {
            var categories = new List<ProductCategory>
            {
                new() { Name = "Phân Bón Hữu Cơ", Slug = "phan-bon-huu-co", Description = "Các loại phân bón hữu cơ tốt cho đất", CreatedAt = DateTime.UtcNow },
                new() { Name = "Phân Bón Hóa Học", Slug = "phan-bon-hoa-hoc", Description = "Phân bón hóa học cho năng suất cao", CreatedAt = DateTime.UtcNow },
                new() { Name = "Thuốc Bảo Vệ Thực Vật", Slug = "thuoc-bao-ve-thuc-vat", Description = "Hóa chất bảo vệ cây trồng", CreatedAt = DateTime.UtcNow },
            };
            await context.ProductCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 4. Tạo Products Mẫu (Nếu chưa có)
        if (!await context.Products.AnyAsync())
        {
            var category = await context.ProductCategories.FirstAsync();
            var products = new List<Product>
            {
                new() { Name = "Phân Urê 46%", Slug = "phan-ure-46", SKU = "URE-001", BasePrice = 350000, StockQuantity = 200, Unit = "Bao 50kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 20, CreatedAt = DateTime.UtcNow },
                new() { Name = "Phân NPK 16-16-8", Slug = "phan-npk-16-16-8", SKU = "NPK-001", BasePrice = 420000, StockQuantity = 150, Unit = "Bao 50kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 15, CreatedAt = DateTime.UtcNow },
                new() { Name = "Phân DAP 18-46-0", Slug = "phan-dap-18-46-0", SKU = "DAP-001", BasePrice = 520000, StockQuantity = 100, Unit = "Bao 50kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 10, CreatedAt = DateTime.UtcNow },
                new() { Name = "Phân Kali (KCl)", Slug = "phan-kali-kcl", SKU = "KAL-001", BasePrice = 480000, StockQuantity = 120, Unit = "Bao 50kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 10, CreatedAt = DateTime.UtcNow },
                new() { Name = "Phân Hữu Cơ Vi Sinh", Slug = "phan-huu-co-vi-sinh", SKU = "HUC-001", BasePrice = 280000, StockQuantity = 80, Unit = "Bao 25kg", CategoryId = category.Id, IsActive = true, MinStockLevel = 10, CreatedAt = DateTime.UtcNow },
            };
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}
