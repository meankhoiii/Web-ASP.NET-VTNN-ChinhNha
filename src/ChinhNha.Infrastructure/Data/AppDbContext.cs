using ChinhNha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ChinhNha.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUserRole> UserRoles => Set<AppUserRole>();

    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<SavedSearchFilter> SavedSearchFilters => Set<SavedSearchFilter>();
    
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<PolicyPage> PolicyPages => Set<PolicyPage>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventoryForecast> InventoryForecasts => Set<InventoryForecast>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Load all IEntityTypeConfiguration from the current assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
