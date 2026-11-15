using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.User;
public class IndexModel(CatalogService catalog) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int? CategoryId { get; set; }
    [BindProperty(SupportsGet = true), Range(0, double.MaxValue)] public decimal? MinPrice { get; set; }
    [BindProperty(SupportsGet = true), Range(0, double.MaxValue)] public decimal? MaxPrice { get; set; }
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; } = "newest";
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public List<Category> Categories { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public int Total { get; set; }
    public int TotalPages => Math.Max(1, (Total + 7) / 8);
    public async Task OnGetAsync()
    {
        PageNumber = Math.Clamp(PageNumber, 1, 100000);
        Categories = await catalog.CategoriesAsync();
        if (MinPrice > MaxPrice) ModelState.AddModelError(string.Empty, "Giá từ không được lớn hơn giá đến.");
        if (!ModelState.IsValid) return;
        var result = await catalog.SearchAsync(Search, CategoryId, MinPrice, MaxPrice, Sort, PageNumber);
        Total = result.Total; Products = result.Products;
        if (PageNumber > TotalPages) { PageNumber = TotalPages; Products = (await catalog.SearchAsync(Search, CategoryId, MinPrice, MaxPrice, Sort, PageNumber)).Products; }
    }
}
