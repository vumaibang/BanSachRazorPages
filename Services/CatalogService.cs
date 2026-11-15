using BanSachRazorPages.Data;
using BanSachRazorPages.Models;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Services;

public record CatalogResult(List<Product> Products, int Total);
public class CatalogService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<Category>> CategoriesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
    }
    public async Task<CatalogResult> SearchAsync(string? search, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, int page = 1)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.IsActive && p.Category.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.ProductName.Contains(search.Trim()) || (p.Author != null && p.Author.Contains(search.Trim())));
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice);
        var count = await query.CountAsync();
        query = sort switch { "price-asc" => query.OrderBy(p => p.Price).ThenBy(p => p.ProductId), "price-desc" => query.OrderByDescending(p => p.Price).ThenBy(p => p.ProductId), _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.ProductId) };
        return new(await query.Skip((Math.Max(1, page) - 1) * 8).Take(8).ToListAsync(), count);
    }
    public async Task<Product?> ProductAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Products.AsNoTracking().Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive && p.Category.IsActive);
    }
}

public static class Display
{
    public static string Money(decimal value) => value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    public static string Status(string value) => value switch { "Pending" => "Chờ xử lý", "Shipping" => "Đang giao", "Completed" => "Hoàn thành", "Cancelled" => "Đã hủy", _ => value };
    public static string Payment(string value) => value == "BankTransfer" ? "Chuyển khoản ngân hàng" : "Thanh toán khi nhận hàng (COD)";
}
