using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin.Products;
public class IndexModel(BanSachRazorPages.Data.AppDbContext db) : PageModel
{
    public List<Product> Items { get; set; } = [];
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int Total { get; set; }
    public int TotalPages => Math.Max(1, (Total + 19) / 20);
    public List<Category> Categories { get; set; } = []; [BindProperty(SupportsGet = true)] public int? CategoryId { get; set; }
    public async Task OnGetAsync()
    {
        Categories = await db.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();
        var query = db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchTerm)) { var keyword = SearchTerm.Trim(); query = query.Where(x => x.ProductName.Contains(keyword) || (x.Author != null && x.Author.Contains(keyword))); }
        if (StatusFilter == "Active") query = query.Where(x => x.IsActive);
        else if (StatusFilter == "Inactive") query = query.Where(x => !x.IsActive);
        if (CategoryId.HasValue) query = query.Where(x => x.CategoryId == CategoryId);
        Total = await query.CountAsync(); PageNumber = Math.Clamp(PageNumber,1,TotalPages);
        Items = await query.OrderByDescending(x => x.ProductId).Skip((PageNumber-1)*20).Take(20).ToListAsync();
    }
    
}
