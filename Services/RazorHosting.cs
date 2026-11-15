using BanSachRazorPages.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Services;

public static class RazorHosting
{
    public static IServiceCollection AddBookStore(this IServiceCollection services, Action<DbContextOptionsBuilder> database)
    {
        services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeFolder("/Admin", "Admin");
            options.Conventions.AllowAnonymousToPage("/Admin/Login");
            options.Conventions.AuthorizeFolder("/User/Orders", "Customer");
            options.Conventions.AuthorizeFolder("/User/Profile", "Customer");
            options.Conventions.AuthorizePage("/User/Cart/Checkout", "Customer");
            options.Conventions.AuthorizePage("/User/Logout");
            options.Conventions.AddPageRoute("/User/Index", "");
            options.Conventions.AddPageRoute("/User/Error", "Error");
        });
        services.AddDistributedMemoryCache();
        services.AddSession(options => { options.Cookie.Name = "BanSachRazor.GuestCart"; options.Cookie.HttpOnly = true; options.Cookie.IsEssential = true; options.IdleTimeout = TimeSpan.FromHours(8); });
        services.AddHttpContextAccessor();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.Name = "BanSachRazor.Auth";
            options.LoginPath = "/User/Login";
            options.AccessDeniedPath = "/User/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.EventsType = typeof(AccountCookieEvents);
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
        });
        services.AddScoped<AccountCookieEvents>();
        services.AddScoped<CurrentUser>();
        services.AddScoped<AccountService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<AdminService>();
        services.AddScoped<OrderService>();
        services.AddScoped<CartService>();
        services.AddDbContextFactory<AppDbContext>(database);
        return services;
    }
    public static void UseBookStore(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/User/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
        // Giữ bookmark cũ; phân quyền dựa trên endpoint đích.
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            string? target = null;
            if (path.StartsWithSegments("/Customer", out var customerPath)) target = "/User" + customerPath;
            else if (path.StartsWithSegments("/Products", out var productPath)) target = "/User/Products" + productPath;
            else if (path.StartsWithSegments("/Cart", out var cartPath)) target = "/User/Cart" + cartPath;
            else if (path.Equals("/Index", StringComparison.OrdinalIgnoreCase)) target = "/";
            else if (path.Equals("/Privacy", StringComparison.OrdinalIgnoreCase)) target = "/User/Privacy";
            if (target != null) { context.Response.Redirect(context.Request.PathBase + target + context.Request.QueryString, permanent: false, preserveMethod: true); return; }
            await next();
        });
        app.MapRazorPages();
    }
}
