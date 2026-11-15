using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Data;
using BanSachRazorPages.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Services;

public sealed class AccountInput
{
    public int AccountId { get; set; }
    [Required, StringLength(50)] public string Username { get; set; } = "";
    [StringLength(100, MinimumLength = 6)] public string? Password { get; set; }
    [Required, RegularExpression("Admin|Customer")] public string Role { get; set; } = "Customer";
    public bool IsActive { get; set; } = true;
}

public record Dashboard(int Products, int Categories, int Customers, int Orders, decimal Revenue, List<Order> RecentOrders);

public class AdminService(IDbContextFactory<AppDbContext> factory, CurrentUser user, IWebHostEnvironment environment)
{
    public async Task<List<T>> ListAsync<T>() where T : class
    {
        await user.RequireAsync("Admin");
        await using var db = await factory.CreateDbContextAsync();
        return await db.Set<T>().AsNoTracking().ToListAsync();
    }
    public async Task<T?> FindAsync<T>(int id) where T : class
    {
        await user.RequireAsync("Admin");
        await using var db = await factory.CreateDbContextAsync();
        return await db.Set<T>().FindAsync(id);
    }
    public async Task<Dashboard> DashboardAsync()
    {
        await user.RequireAsync("Admin");
        await using var db = await factory.CreateDbContextAsync();
        return new(await db.Products.CountAsync(), await db.Categories.CountAsync(), await db.Customers.CountAsync(),
            await db.Orders.CountAsync(), await db.Orders.Where(o => o.Status == "Completed").SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            await db.Orders.AsNoTracking().Include(o => o.Customer).OrderByDescending(o => o.OrderDate).Take(8).ToListAsync());
    }

    public async Task SaveAsync<T>(T input, int id) where T : class
    {
        await user.RequireAsync("Admin");
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var db = await factory.CreateDbContextAsync();
        switch (input)
        {
            case Product p:
                if (p.Price < 0 || p.StockQuantity < 0 || p.CategoryId <= 0) throw new ValidationException("Giá, tồn kho và danh mục không hợp lệ.");
                if (!await db.Categories.AnyAsync(c => c.CategoryId == p.CategoryId)) throw new ValidationException("Danh mục không tồn tại.");
                if (p.PublishYear is < 1 or > 9999) throw new ValidationException("Năm xuất bản không hợp lệ.");
                p.ProductName = p.ProductName.Trim();
                break;
            case Category c:
                c.CategoryName = c.CategoryName.Trim();
                if (await db.Categories.AnyAsync(x => x.CategoryName == c.CategoryName && x.CategoryId != id)) throw new ValidationException("Tên danh mục đã tồn tại.");
                break;
            case Customer c:
                c.FullName = c.FullName.Trim();
                if (!string.IsNullOrEmpty(c.Email) && !new EmailAddressAttribute().IsValid(c.Email)) throw new ValidationException("Email không hợp lệ.");
                break;
            default: throw new ValidationException("Loại dữ liệu không được hỗ trợ.");
        }
        if (id == 0) db.Add(input);
        else
        {
            var entity = await db.Set<T>().FindAsync(id) ?? throw new ValidationException("Không tìm thấy dữ liệu.");
            // Không nhận khóa tài khoản hoặc thời gian tạo từ biểu mẫu chỉnh sửa khách hàng.
            if (input is Customer customer && entity is Customer original) { customer.AccountId = original.AccountId; customer.CreatedAt = original.CreatedAt; }
            if (input is Product product && entity is Product oldProduct) product.CreatedAt = oldProduct.CreatedAt;
            db.Entry(entity).CurrentValues.SetValues(input);
        }
        await db.SaveChangesAsync();
    }

    public async Task SaveAccountAsync(AccountInput input)
    {
        var current = await user.RequireAsync("Admin");
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (await db.Accounts.AnyAsync(a => a.Username == input.Username.Trim() && a.AccountId != input.AccountId)) throw new ValidationException("Tên đăng nhập đã tồn tại.");
        var account = input.AccountId == 0 ? new Account() : await db.Accounts.Include(a => a.Customer).FirstOrDefaultAsync(a => a.AccountId == input.AccountId)
            ?? throw new ValidationException("Không tìm thấy tài khoản.");
        if (account.AccountId == current.AccountId && (!input.IsActive || input.Role != "Admin")) throw new ValidationException("Không thể khóa hoặc hạ quyền tài khoản đang đăng nhập.");
        if (account.Role == "Admin" && account.IsActive && (!input.IsActive || input.Role != "Admin") && await db.Accounts.CountAsync(a => a.Role == "Admin" && a.IsActive) <= 1)
            throw new ValidationException("Không thể khóa hoặc hạ quyền Admin cuối cùng.");
        if (input.AccountId == 0 && string.IsNullOrWhiteSpace(input.Password)) throw new ValidationException("Nhập mật khẩu cho tài khoản mới.");
        account.Username = input.Username.Trim();
        account.Role = input.Role;
        account.IsActive = input.IsActive;
        if (!string.IsNullOrEmpty(input.Password)) account.PasswordHash = new PasswordHasher<Account>().HashPassword(account, input.Password);
        if (account.Role == "Customer" && account.Customer == null) account.Customer = new Customer { FullName = account.Username };
        if (input.AccountId == 0) db.Accounts.Add(account);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<string> DeleteAsync<T>(int id) where T : class
    {
        var current = await user.RequireAsync("Admin");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var entity = await db.Set<T>().FindAsync(id) ?? throw new ValidationException("Dữ liệu không còn tồn tại.");
        var message = "Đã xóa dữ liệu.";
        switch (entity)
        {
            case Product product:
                if (await db.OrderDetails.AnyAsync(d => d.ProductId == id)) { product.IsActive = false; message = "Sách đã có đơn hàng nên được chuyển sang ngừng bán."; }
                else { db.CartItems.RemoveRange(await db.CartItems.Where(c => c.ProductId == id).ToListAsync()); db.Products.Remove(product); }
                break;
            case Category:
                if (await db.Products.AnyAsync(p => p.CategoryId == id)) throw new ValidationException("Không thể xóa danh mục đang chứa sách.");
                db.Remove(entity);
                break;
            case Customer customer:
                if (await db.Orders.AnyAsync(o => o.CustomerId == id)) throw new ValidationException("Không thể xóa khách hàng đã có đơn hàng.");
                if (customer.AccountId.HasValue)
                {
                    var linked = await db.Accounts.FindAsync(customer.AccountId.Value);
                    if (linked?.Role == "Admin") throw new ValidationException("Khách hàng đang liên kết với tài khoản Admin.");
                    if (linked != null) linked.IsActive = false;
                }
                db.Remove(entity);
                break;
            case Account account:
                if (current.AccountId == id) throw new ValidationException("Không thể xóa tài khoản đang đăng nhập.");
                if (account.Role == "Admin" && account.IsActive && await db.Accounts.CountAsync(a => a.Role == "Admin" && a.IsActive) <= 1) throw new ValidationException("Không thể xóa Admin cuối cùng.");
                if (await db.Customers.AnyAsync(c => c.AccountId == id)) throw new ValidationException("Tài khoản đã liên kết với khách hàng; có thể khóa thay vì xóa.");
                db.Remove(entity);
                break;
            default: throw new ValidationException("Không hỗ trợ xóa dữ liệu này.");
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return message;
    }

    public async Task<string> UploadCoverAsync(IFormFile file)
    {
        await user.RequireAsync("Admin");
        const long maxSize = 5 * 1024 * 1024;
        if (file.Length > maxSize) throw new ValidationException("Ảnh tối đa 5 MB.");
        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        var extension = bytes.Length >= 8 && bytes.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ? ".png"
            : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg"
            : bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" ? ".webp" : null;
        if (extension is null) throw new ValidationException("Chỉ hỗ trợ ảnh PNG, JPEG hoặc WebP hợp lệ.");
        var folder = Path.Combine(environment.WebRootPath, "images", "products");
        Directory.CreateDirectory(folder);
        var filename = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(folder, filename), bytes);
        return "/images/products/" + filename;
    }
}
