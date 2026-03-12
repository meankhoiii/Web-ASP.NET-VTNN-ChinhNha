using ChinhNha.Application.Interfaces;
using ChinhNha.Application.Mappings;
using ChinhNha.Application.Services;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;
using ChinhNha.Infrastructure.Repositories;
using ChinhNha.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Setup Entity Framework and SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("ChinhNha.Infrastructure")));

// Setup custom cookie auth
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                var returnUrl = Uri.EscapeDataString(context.Request.GetEncodedPathAndQuery());
                var isAdminArea = context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase);
                var redirectPath = isAdminArea
                    ? $"/Admin/Auth/Login?returnUrl={returnUrl}"
                    : $"/Account/Login?returnUrl={returnUrl}";

                context.Response.Redirect(redirectPath);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                var returnUrl = Uri.EscapeDataString(context.Request.GetEncodedPathAndQuery());
                var isAdminArea = context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase);
                var redirectPath = isAdminArea
                    ? $"/Admin/Auth/Login?returnUrl={returnUrl}"
                    : $"/Account/Login?returnUrl={returnUrl}";

                context.Response.Redirect(redirectPath);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ---- Repositories ----
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();

// ---- Domain Services ----
builder.Services.AddScoped<IAppUserService, AppUserService>();

// ---- Application Services ----
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IInventoryImportExportService, InventoryImportExportService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IVNPayService, ChinhNha.Infrastructure.Services.VNPay.VNPayService>();
builder.Services.AddScoped<IAuthService, CookieAuthService>();
builder.Services.AddScoped<IPasswordHashService, Pbkdf2PasswordHashService>();
builder.Services.AddScoped<IAiModelSelectionService, AiModelSelectionService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// ---- AI / ML.NET Service ----
builder.Services.AddSingleton<IInventoryForecastService, DemandForecastService>();

// ---- AutoMapper ----
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Session (for guest cart)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<UnitOfWork>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Route for Admin Area
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// SEO Routes
app.MapControllerRoute(
    name: "productDetails",
    pattern: "san-pham/{slug}",
    defaults: new { controller = "Product", action = "Details" });

app.MapControllerRoute(
    name: "productCategory",
    pattern: "danh-muc/{slug}",
    defaults: new { controller = "Product", action = "ByCategory" });

app.MapControllerRoute(
    name: "blogDetails",
    pattern: "tin-tuc/{slug}",
    defaults: new { controller = "Blog", action = "Details" });

// Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Config for Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var passwordHashService = services.GetRequiredService<IPasswordHashService>();
        
        await context.Database.MigrateAsync(); // Ensure DB is completely migrated
        await DbSeeder.SeedAsync(context, passwordHashService);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
