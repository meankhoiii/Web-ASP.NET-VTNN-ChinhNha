using ChinhNha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ChinhNha.Application.Interfaces;

namespace ChinhNha.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHashService passwordHashService)
    {
        // 1. Tạo Roles
        if (!await context.Roles.AnyAsync(r => r.Name == "Admin"))
        {
            context.Roles.Add(new Role { Name = "Admin" });
        }

        if (!await context.Roles.AnyAsync(r => r.Name == "Customer"))
        {
            context.Roles.Add(new Role { Name = "Customer" });
        }

        await context.SaveChangesAsync();

        // 2. Tạo Admin User
        var adminUser = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == "admin@chinhnha.id.vn");

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                Email = "admin@chinhnha.id.vn",
                FullName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            adminUser.PasswordHash = passwordHashService.HashPassword("Admin@123");
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }

        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
        var hasAdminRole = await context.UserRoles.AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
        if (!hasAdminRole)
        {
            context.UserRoles.Add(new AppUserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
            await context.SaveChangesAsync();
        }

        // 3. Tạo Supplier Mẫu (Nếu chưa có)
        if (!await context.Suppliers.AnyAsync())
        {
            var suppliers = new List<Supplier>
            {
                new() { Name = "Công ty CP Phân bón Việt Nam", Phone = "0901234567", Email = "lh@cpvn.com.vn", Address = "TP. Hồ Chí Minh", IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { Name = "Công ty TNHH Agri Miền Nam", Phone = "0907654321", Email = "sales@agrimn.vn", Address = "Cần Thơ", IsActive = true, CreatedAt = DateTime.UtcNow },
            };
            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();
        }

        // 4. Tạo Category Mẫu (Nếu chưa có)
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

        // 5. Tạo Blog Category Mẫu (Nếu chưa có)
        if (!await context.BlogCategories.AnyAsync())
        {
            var blogCats = new List<BlogCategory>
            {
                new() { Name = "Kiến thức Nông nghiệp", Slug = "kien-thuc-nong-nghiep", Description = "Chia sẻ kiến thức trồng trọt, chăm sóc cây", DisplayOrder = 1 },
                new() { Name = "Tin tức thị trường", Slug = "tin-tuc-thi-truong", Description = "Cập nhật giá phân bón, thông tin thị trường", DisplayOrder = 2 },
                new() { Name = "Kinh nghiệm sử dụng phân bón", Slug = "kinh-nghiem-su-dung-phan-bon", Description = "Hướng dẫn sử dụng phân bón hiệu quả", DisplayOrder = 3 },
            };
            await context.BlogCategories.AddRangeAsync(blogCats);
            await context.SaveChangesAsync();
        }

        // 6. Tạo Products Mẫu (Nếu chưa có)
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
