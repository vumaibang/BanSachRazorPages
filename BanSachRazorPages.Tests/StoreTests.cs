using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BanSachRazorPages.Data;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace BanSachRazorPages.Tests;

public sealed class StoreTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private DbFactory factory = null!;
    private TestHttpAccessor accessor = null!;
    private CurrentUser user = null!;
    private OrderService orders = null!;
    private AdminService admin = null!;
    private Account customer = null!, administrator = null!, other = null!;
    private Product book = null!, secondBook = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        factory = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await using var db = factory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        Account Make(string name, string role) {
            var a = new Account { Username = name, Role = role };
            a.PasswordHash = new PasswordHasher<Account>().HashPassword(a, "Test@123");
            if (role == "Customer") a.Customer = new Customer { FullName = name, Address = "123 Đường Sách", Phone = "0901234567" };
            return a;
        }
        customer = Make("customer", "Customer"); other = Make("other", "Customer"); administrator = Make("admin", "Admin");
        var category = new Category { CategoryName = "Sách" };
        book = new Product { ProductName = "Sách kiểm thử", Price = 100000, StockQuantity = 10, Category = category };
        secondBook = new Product { ProductName = "Sách hết hàng", Price = 50000, StockQuantity = 0, Category = category };
        db.AddRange(customer, other, administrator, book, secondBook);
        await db.SaveChangesAsync();
        accessor = new() { HttpContext = new DefaultHttpContext { Session = new TestSession() } }; SignIn(customer);
        user = new(accessor, factory);
        orders = new(factory, user);
        admin = new(factory, user, new TestEnvironment());
    }
    public async Task DisposeAsync() => await connection.DisposeAsync();
    private void SignIn(Account account) => accessor.HttpContext!.User = CurrentUser.Principal(account);
    private static CheckoutInput Input() => new() { ShippingAddress = "123 Đường Sách", Phone = "0901234567" };
    private async Task AddCart(Product product, int quantity)
    {
        await using var db = factory.CreateDbContext();
        db.CartItems.Add(new() { CustomerId = customer.Customer!.CustomerId, ProductId = product.ProductId, Quantity = quantity });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GuestCartSurvivesNewRequestAndMergesOnLogin()
    {
        accessor.HttpContext!.User = new(new ClaimsIdentity());
        var cart = new CartService(factory, user, accessor);
        await cart.AddAsync(book.ProductId, 2);
        // Một service mới đại diện cho request mới, vẫn dùng session của trình duyệt.
        cart = new(factory, user, accessor);
        Assert.Equal(2, (await cart.GetAsync()).Single().Quantity);
        SignIn(customer);
        Assert.Equal(2, (await cart.GetAsync()).Single().Quantity);
        Assert.Equal(2, (await cart.GetAsync()).Single().Quantity);
        await using var db = factory.CreateDbContext();
        Assert.Equal(customer.Customer!.CustomerId, (await db.CartItems.SingleAsync()).CustomerId);
    }

    [Fact]
    public async Task GuestCartRejectsExcessStockAndRemovesUnavailableBook()
    {
        accessor.HttpContext!.User = new(new ClaimsIdentity());
        var cart = new CartService(factory, user, accessor);
        await Assert.ThrowsAsync<ValidationException>(() => cart.AddAsync(book.ProductId, 11));
        await cart.AddAsync(book.ProductId);
        await using (var db = factory.CreateDbContext()) { var p = await db.Products.FindAsync(book.ProductId); p!.IsActive = false; await db.SaveChangesAsync(); }
        Assert.Equal(0, (await cart.GetAsync()).Single().StockQuantity);
        await cart.UpdateAsync(book.ProductId, 0);
        Assert.Empty(await cart.GetAsync());
    }

    [Fact]
    public async Task CheckoutUsesDatabasePriceAndClearsOnlyOwnCart()
    {
        await AddCart(book, 2);
        await using (var db = factory.CreateDbContext()) {
            db.CartItems.Add(new() { CustomerId = other.Customer!.CustomerId, ProductId = book.ProductId, Quantity = 1 });
            await db.SaveChangesAsync();
        }
        var id = await orders.CheckoutAsync(Input());
        await using var check = factory.CreateDbContext();
        var order = await check.Orders.Include(o => o.OrderDetails).SingleAsync(o => o.OrderId == id);
        Assert.Equal(200000m, order.TotalAmount);
        Assert.Equal(customer.Customer!.CustomerId, order.CustomerId);
        Assert.Equal(8, (await check.Products.FindAsync(book.ProductId))!.StockQuantity);
        Assert.Empty(await check.CartItems.Where(c => c.CustomerId == customer.Customer.CustomerId).ToListAsync());
        Assert.Single(await check.CartItems.Where(c => c.CustomerId == other.Customer!.CustomerId).ToListAsync());
    }

    [Fact]
    public async Task InsufficientStockRollsBackAllLinesAndKeepsCart()
    {
        await AddCart(book, 2); await AddCart(secondBook, 1);
        await Assert.ThrowsAsync<ValidationException>(() => orders.CheckoutAsync(Input()));
        await using var db = factory.CreateDbContext();
        Assert.Equal(10, (await db.Products.FindAsync(book.ProductId))!.StockQuantity);
        Assert.Empty(await db.Orders.ToListAsync()); Assert.Equal(2, await db.CartItems.CountAsync());
    }

    [Fact]
    public async Task BuyNowKeepsExistingCart()
    {
        await AddCart(book, 3);
        await orders.CheckoutAsync(Input(), book.ProductId, 1);
        await using var db = factory.CreateDbContext();
        Assert.Equal(3, (await db.CartItems.SingleAsync()).Quantity);
        Assert.Equal(9, (await db.Products.FindAsync(book.ProductId))!.StockQuantity);
    }

    [Fact]
    public async Task CancellationRestocksOnlyOnce()
    {
        var id = await orders.CheckoutAsync(Input(), book.ProductId, 2);
        await orders.ChangeStatusAsync(id, "Cancelled");
        await Assert.ThrowsAsync<ValidationException>(() => orders.ChangeStatusAsync(id, "Cancelled"));
        await using var db = factory.CreateDbContext();
        Assert.Equal(10, (await db.Products.FindAsync(book.ProductId))!.StockQuantity);
        Assert.Equal("Cancelled", (await db.Orders.FindAsync(id))!.Status);
    }

    [Fact]
    public async Task CustomerCannotReadOrCancelAnotherCustomersOrder()
    {
        var id = await orders.CheckoutAsync(Input(), book.ProductId);
        SignIn(other);
        Assert.Null(await orders.FindAsync(id)); Assert.Empty(await orders.ListAsync());
        await Assert.ThrowsAsync<ValidationException>(() => orders.ChangeStatusAsync(id, "Cancelled"));
        await using var db = factory.CreateDbContext(); Assert.Equal(9, (await db.Products.FindAsync(book.ProductId))!.StockQuantity);
    }

    [Fact]
    public async Task OnlyAdminCanCancelShippingAndReadAllOrders()
    {
        var id = await orders.CheckoutAsync(Input(), book.ProductId, 2);
        SignIn(administrator);
        Assert.NotNull(await orders.FindAsync(id, true));
        await orders.ChangeStatusAsync(id, "Shipping", true);
        SignIn(customer);
        await Assert.ThrowsAsync<ValidationException>(() => orders.ChangeStatusAsync(id, "Cancelled"));
        SignIn(administrator);
        await orders.ChangeStatusAsync(id, "Cancelled", true);
        await using var db = factory.CreateDbContext(); Assert.Equal(10, (await db.Products.FindAsync(book.ProductId))!.StockQuantity);
    }

    [Fact]
    public async Task CompletedOrderCannotBeCancelled()
    {
        var id = await orders.CheckoutAsync(Input(), book.ProductId);
        SignIn(administrator);
        await orders.ChangeStatusAsync(id, "Shipping", true);
        await orders.ChangeStatusAsync(id, "Completed", true);
        await Assert.ThrowsAsync<ValidationException>(() => orders.ChangeStatusAsync(id, "Cancelled", true));
        await using var db = factory.CreateDbContext(); Assert.Equal(9, (await db.Products.FindAsync(book.ProductId))!.StockQuantity);
    }

    [Fact]
    public async Task CustomerCannotUseAdminService()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => admin.ListAsync<Account>());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => admin.DeleteAsync<Product>(book.ProductId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => orders.ListAsync(true));
    }

    [Fact]
    public async Task LockedAccountIsRejectedByExistingSession()
    {
        await using (var db = factory.CreateDbContext()) { var a = await db.Accounts.FindAsync(customer.AccountId); a!.IsActive = false; await db.SaveChangesAsync(); }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => orders.CheckoutAsync(Input(), book.ProductId));
    }

    [Fact]
    public async Task ChangingPasswordInvalidatesOldPrincipal()
    {
        await orders.ChangePasswordAsync(new() { CurrentPassword = "Test@123", NewPassword = "New@123", ConfirmPassword = "New@123" });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => user.RequireAsync("Customer"));
        var accounts = new AccountService(factory);
        Assert.Null(await accounts.LoginAsync("customer", "Test@123"));
        Assert.NotNull(await accounts.LoginAsync("customer", "New@123"));
    }

    [Fact]
    public async Task ProductWithOrdersIsDeactivatedInsteadOfDeleted()
    {
        await orders.CheckoutAsync(Input(), book.ProductId);
        SignIn(administrator);
        await admin.DeleteAsync<Product>(book.ProductId);
        await using var db = factory.CreateDbContext();
        Assert.False((await db.Products.FindAsync(book.ProductId))!.IsActive);
        Assert.Single(await db.OrderDetails.ToListAsync());
    }

    [Fact]
    public async Task AdminCannotDeleteSelfOrDemoteSelf()
    {
        SignIn(administrator);
        await Assert.ThrowsAsync<ValidationException>(() => admin.DeleteAsync<Account>(administrator.AccountId));
        await Assert.ThrowsAsync<ValidationException>(() => admin.SaveAccountAsync(new() { AccountId = administrator.AccountId, Username = "admin", Role = "Customer" }));
    }

    [Fact]
    public async Task RegistrationCreatesLinkedCustomerAndRejectsDuplicate()
    {
        var service = new AccountService(factory);
        var input = new RegistrationInput { Username = "reader", Password = "Reader@123", ConfirmPassword = "Reader@123", FullName = "Bạn đọc" };
        await service.RegisterAsync(input);
        var account = await service.LoginAsync("reader", "Reader@123");
        Assert.Equal("Customer", account!.Role); Assert.Equal("Bạn đọc", account.Customer!.FullName);
        await Assert.ThrowsAsync<ValidationException>(() => service.RegisterAsync(input));
    }

    [Fact]
    public async Task HiddenCategoryIsNotSold()
    {
        await using (var db = factory.CreateDbContext()) { var category = await db.Categories.FindAsync(book.CategoryId); category!.IsActive = false; await db.SaveChangesAsync(); }
        Assert.Null(await new CatalogService(factory).ProductAsync(book.ProductId));
        await Assert.ThrowsAsync<ValidationException>(() => orders.CheckoutAsync(Input(), book.ProductId));
    }

    private sealed class DbFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    { public AppDbContext CreateDbContext() => new(options); }
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
    }
}
