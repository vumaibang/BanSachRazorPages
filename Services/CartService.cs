using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BanSachRazorPages.Data;
using BanSachRazorPages.Models;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Services;

public record CartQuantity(int ProductId, int Quantity);

public class CartService(IDbContextFactory<AppDbContext> factory, CurrentUser user, IHttpContextAccessor accessor)
{
    private const string GuestKey = "Cart";
    private ISession Session => accessor.HttpContext!.Session;
    private List<CartQuantity> Guest()
    {
        try { return JsonSerializer.Deserialize<List<CartQuantity>>(Session.GetString(GuestKey) ?? "[]")?.Where(x => x.Quantity > 0).ToList() ?? []; }
        catch (JsonException) { Session.Remove(GuestKey); return []; }
    }
    public async Task<List<CartItem>> GetAsync()
    {
        var account = await user.OptionalAsync();
        if (account?.Role == "Admin") throw new ValidationException("Vui lòng dùng tài khoản khách hàng để mua sách.");
        await using var db = await factory.CreateDbContextAsync();
        if (account?.Customer is { } customer)
        {
            await MergeAsync(db, customer.CustomerId);
            return await db.CartItems.AsNoTracking().Where(c => c.CustomerId == customer.CustomerId)
                .Select(c => new CartItem { ProductId = c.ProductId, ProductName = c.Product.ProductName, Price = c.Product.Price,
                    Quantity = c.Quantity, StockQuantity = c.Product.IsActive && c.Product.Category.IsActive ? c.Product.StockQuantity : 0, ImageUrl = c.Product.ImageUrl }).ToListAsync();
        }
        var guest = Guest();
        var ids = guest.Select(g => g.ProductId).ToList();
        var products = await db.Products.AsNoTracking().Include(p => p.Category).Where(p => ids.Contains(p.ProductId)).ToListAsync();
        return guest.Where(g => products.Any(p => p.ProductId == g.ProductId)).Select(g =>
        {
            var p = products.First(p => p.ProductId == g.ProductId);
            return new CartItem { ProductId = p.ProductId, ProductName = p.ProductName, Price = p.Price, Quantity = g.Quantity,
                StockQuantity = p.IsActive && p.Category.IsActive ? p.StockQuantity : 0, ImageUrl = p.ImageUrl };
        }).ToList();
    }
    public Task AddAsync(int id, int quantity = 1) => SetAsync(id, quantity, true);
    public Task UpdateAsync(int id, int quantity) => SetAsync(id, quantity, false);
    private async Task SetAsync(int id, int quantity, bool add)
    {
        if (quantity < 0 || (add && quantity == 0)) throw new ValidationException("Số lượng phải lớn hơn 0.");
        var account = await user.OptionalAsync();
        if (account?.Role == "Admin") throw new ValidationException("Vui lòng dùng tài khoản khách hàng để mua sách.");
        await using var db = await factory.CreateDbContextAsync();
        if (account?.Customer is { } customer)
        {
            await MergeAsync(db, customer.CustomerId);
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == id);
            var item = await db.CartItems.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId && c.ProductId == id);
            var total = (long)quantity + (add ? item?.Quantity ?? 0 : 0);
            Validate(product, total);
            if (total == 0) { if (item != null) db.CartItems.Remove(item); }
            else if (item == null) db.CartItems.Add(new() { CustomerId = customer.CustomerId, ProductId = id, Quantity = (int)total });
            else item.Quantity = (int)total;
            await db.SaveChangesAsync(); await transaction.CommitAsync();
        }
        else
        {
            var guest = Guest();
            var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == id);
            var total = (long)quantity + (add ? guest.FirstOrDefault(g => g.ProductId == id)?.Quantity ?? 0 : 0);
            Validate(product, total);
            guest.RemoveAll(g => g.ProductId == id);
            if (total > 0) guest.Add(new(id, (int)total));
            Session.SetString(GuestKey, JsonSerializer.Serialize(guest));
        }
    }
    public async Task ClearAsync()
    {
        var account = await user.OptionalAsync();
        if (account?.Role == "Admin") throw new ValidationException("Vui lòng dùng tài khoản khách hàng để mua sách.");
        if (account?.Customer is { } customer)
        {
            await using var db = await factory.CreateDbContextAsync();
            await db.CartItems.Where(c => c.CustomerId == customer.CustomerId).ExecuteDeleteAsync();
        }
        Session.Remove(GuestKey);
    }
    private static void Validate(Product? product, long quantity)
    {
        if (quantity == 0) return;
        if (product is null || !product.IsActive || !product.Category.IsActive) throw new ValidationException("Sách hiện không còn kinh doanh.");
        if (quantity > product.StockQuantity) throw new ValidationException($"Sách '{product.ProductName}' chỉ còn {product.StockQuantity} cuốn.");
    }
    private async Task MergeAsync(AppDbContext db, int customerId)
    {
        var guest = Guest();
        if (guest.Count == 0) return;
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        foreach (var line in guest.OrderBy(g => g.ProductId))
        {
            var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == line.ProductId);
            if (product is null || !product.IsActive || !product.Category.IsActive || product.StockQuantity <= 0) continue;
            var item = await db.CartItems.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.ProductId == line.ProductId);
            if (item == null) db.CartItems.Add(new() { CustomerId = customerId, ProductId = line.ProductId, Quantity = Math.Min(line.Quantity, product.StockQuantity) });
            else item.Quantity = Math.Min(Math.Max(item.Quantity, line.Quantity), product.StockQuantity);
        }
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        Session.Remove(GuestKey);
    }
}
