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
            var category = new ProductCategory
            {
                Name = "Phân Bón Hữu Cơ",
                Slug = "phan-bon-huu-co",
                Description = "Các loại phân bón hữu cơ tốt cho đất",
                CreatedAt = DateTime.UtcNow
            };
            await context.ProductCategories.AddAsync(category);
            await context.SaveChangesAsync();
        }
    }
}
