using BanSachRazorPages.Data;
using BanSachRazorPages.Services;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Identity;
using BanSachRazorPages.Models;
using BanSachRazorPages.Pages.Admin.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Tests;

public class AdminOrderCancelStockTests
{
    [Fact]
    public async Task Cancelling_a_pending_order_returns_stock()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var product = await SeedOrderAsync(db, status: "Pending", stock: 5, quantity: 2);
        var page = await CreatePageAsync(db, options);

        await page.OnPostUpdateStatusAsync(id: 1, newStatus: "Cancelled");

        db.ChangeTracker.Clear();
        var updatedProduct = await db.Products.SingleAsync(p => p.ProductId == product.ProductId);
        var updatedOrder = await db.Orders.SingleAsync();

        Assert.Equal(7, updatedProduct.StockQuantity);
        Assert.Equal("Cancelled", updatedOrder.Status);
    }

    [Fact]
    public async Task Completing_a_shipping_order_does_not_change_stock()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var product = await SeedOrderAsync(db, status: "Shipping", stock: 5, quantity: 2);
        var page = await CreatePageAsync(db, options);

        await page.OnPostUpdateStatusAsync(id: 1, newStatus: "Completed");

        db.ChangeTracker.Clear();
        var updatedProduct = await db.Products.SingleAsync(p => p.ProductId == product.ProductId);
        var updatedOrder = await db.Orders.SingleAsync();

        Assert.Equal(5, updatedProduct.StockQuantity);
        Assert.Equal("Completed", updatedOrder.Status);
    }

    private sealed class DbFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    { public AppDbContext CreateDbContext() => new(options); }

    private static async Task<DetailsModel> CreatePageAsync(AppDbContext db, DbContextOptions<AppDbContext> options)
    {
        var admin = new Account { Username = "admin", Role = "Admin" };
        admin.PasswordHash = new PasswordHasher<Account>().HashPassword(admin, "Admin@123");
        db.Accounts.Add(admin);
        await db.SaveChangesAsync();
        var httpContext = new DefaultHttpContext { User = CurrentUser.Principal(admin) };
        var factory = new DbFactory(options);
        var service = new OrderService(factory, new CurrentUser(new TestHttpAccessor { HttpContext = httpContext }, factory));
        return new DetailsModel(service) { TempData = new TempDataDictionary(httpContext, new EmptyTempDataProvider()) };
    }

    private static async Task<Product> SeedOrderAsync(
        AppDbContext db,
        string status,
        int stock,
        int quantity)
    {
        var category = new Category { CategoryName = "Sách" };
        var product = new Product
        {
            Category = category,
            ProductName = "Dế Mèn",
            Price = 50000,
            StockQuantity = stock,
            IsActive = true
        };
        var customer = new Customer { FullName = "Bằng", IsActive = true };
        var order = new Order
        {
            Customer = customer,
            Status = status,
            TotalAmount = product.Price * quantity,
            OrderDetails =
            {
                new OrderDetail
                {
                    Product = product,
                    Quantity = quantity,
                    UnitPrice = product.Price
                }
            }
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return product;
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
