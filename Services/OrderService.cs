using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Data;
using BanSachRazorPages.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Services;

public sealed class CheckoutInput
{
    [Required(ErrorMessage = "Nhập địa chỉ giao hàng."), StringLength(255)] public string ShippingAddress { get; set; } = "";
    [Required(ErrorMessage = "Nhập số điện thoại."), StringLength(20)] public string Phone { get; set; } = "";
    [StringLength(500)] public string? Note { get; set; }
    [Required, RegularExpression("COD|BankTransfer")] public string PaymentMethod { get; set; } = "COD";
}

public class OrderService(IDbContextFactory<AppDbContext> factory, CurrentUser user)
{
    public async Task<Customer> ProfileAsync() => (await user.RequireAsync("Customer")).Customer!;
    public async Task SaveProfileAsync(Customer input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        if (!string.IsNullOrEmpty(input.Email) && !new EmailAddressAttribute().IsValid(input.Email)) throw new ValidationException("Email không hợp lệ.");
        var account = await user.RequireAsync("Customer");
        await using var db = await factory.CreateDbContextAsync();
        var customer = await db.Customers.SingleAsync(c => c.AccountId == account.AccountId);
        customer.FullName = input.FullName.Trim(); customer.Phone = input.Phone?.Trim(); customer.Email = input.Email?.Trim(); customer.Address = input.Address?.Trim();
        await db.SaveChangesAsync();
    }
    public async Task ChangePasswordAsync(PasswordInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        var current = await user.RequireAsync("Customer");
        await using var db = await factory.CreateDbContextAsync();
        var account = await db.Accounts.SingleAsync(a => a.AccountId == current.AccountId);
        var hasher = new PasswordHasher<Account>();
        if (hasher.VerifyHashedPassword(account, account.PasswordHash, input.CurrentPassword) == PasswordVerificationResult.Failed) throw new ValidationException("Mật khẩu hiện tại không đúng.");
        account.PasswordHash = hasher.HashPassword(account, input.NewPassword);
        await db.SaveChangesAsync();
    }
    public async Task<List<Order>> ListAsync(bool admin = false)
    {
        var account = await user.RequireAsync(admin ? "Admin" : "Customer");
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Orders.AsNoTracking().Include(o => o.Customer).AsQueryable();
        if (!admin) query = query.Where(o => o.CustomerId == account.Customer!.CustomerId);
        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }
    public async Task<Order?> FindAsync(int id, bool admin = false)
    {
        var account = await user.RequireAsync(admin ? "Admin" : "Customer");
        var customerId = account.Customer?.CustomerId ?? 0;
        await using var db = await factory.CreateDbContextAsync();
        return await db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id && (admin || o.CustomerId == customerId));
    }

    // Giá và số lượng lấy lại từ DB; không tin giá gửi từ giao diện.
    public async Task<int> CheckoutAsync(CheckoutInput input, int? buyNowProductId = null, int buyNowQuantity = 1)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        var account = await user.RequireAsync("Customer");
        var customerId = account.Customer!.CustomerId;
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var lines = buyNowProductId.HasValue ? new List<CartQuantity> { new(buyNowProductId.Value, buyNowQuantity) }
            : await db.CartItems.Where(c => c.CustomerId == customerId).Select(c => new CartQuantity(c.ProductId, c.Quantity)).ToListAsync();
        if (lines.Count == 0) throw new ValidationException("Giỏ hàng đang trống.");
        var order = new Order { CustomerId = customerId, ShippingAddress = input.ShippingAddress.Trim(), Phone = input.Phone.Trim(), Note = input.Note?.Trim(), PaymentMethod = input.PaymentMethod };
        foreach (var line in lines.OrderBy(l => l.ProductId))
        {
            if (line.Quantity <= 0) throw new ValidationException("Số lượng đặt mua không hợp lệ.");
            var product = await db.Products.AsNoTracking().Include(p => p.Category).SingleOrDefaultAsync(p => p.ProductId == line.ProductId);
            if (product is null || !product.IsActive || !product.Category.IsActive) throw new ValidationException("Có sách không còn kinh doanh. Vui lòng kiểm tra lại giỏ.");
            // UPDATE có điều kiện tránh bán vượt tồn kho khi nhiều khách đặt đồng thời.
            var updated = await db.Products.Where(p => p.ProductId == line.ProductId && p.StockQuantity >= line.Quantity && p.IsActive)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.StockQuantity, p => p.StockQuantity - line.Quantity));
            if (updated != 1) throw new ValidationException($"Sách '{product.ProductName}' không đủ tồn kho.");
            order.OrderDetails.Add(new OrderDetail { ProductId = line.ProductId, Quantity = line.Quantity, UnitPrice = product.Price });
            order.TotalAmount += product.Price * line.Quantity;
        }
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        if (!buyNowProductId.HasValue) await db.CartItems.Where(c => c.CustomerId == customerId).ExecuteDeleteAsync();
        await transaction.CommitAsync();
        return order.OrderId;
    }

    public static bool CanTransition(string current, string next, bool admin) => admin
        ? current switch { "Pending" => next is "Shipping" or "Cancelled", "Shipping" => next is "Completed" or "Cancelled", _ => false }
        : current == "Pending" && next == "Cancelled";

    public async Task ChangeStatusAsync(int id, string next, bool admin = false)
    {
        var account = await user.RequireAsync(admin ? "Admin" : "Customer");
        var customerId = account.Customer?.CustomerId ?? 0;
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var order = await db.Orders.AsNoTracking().Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.OrderId == id && (admin || o.CustomerId == customerId))
            ?? throw new ValidationException("Không tìm thấy đơn hàng.");
        if (!CanTransition(order.Status, next, admin)) throw new ValidationException("Không thể chuyển trạng thái đơn hàng này.");
        var updated = await db.Orders.Where(o => o.OrderId == id && o.Status == order.Status).ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, next));
        if (updated != 1) throw new ValidationException("Đơn hàng vừa được cập nhật. Vui lòng tải lại.");
        if (next == "Cancelled")
            foreach (var detail in order.OrderDetails.OrderBy(d => d.ProductId))
                await db.Products.Where(p => p.ProductId == detail.ProductId).ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + detail.Quantity));
        await transaction.CommitAsync();
    }
}
