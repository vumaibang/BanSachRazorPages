using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin.Customers;
public class IndexModel(BanSachRazorPages.Data.AppDbContext db, AdminService admin) : PageModel
{
    public List<Customer> Items { get; set; } = [];
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int Total { get; set; }
    public int TotalPages => Math.Max(1, (Total + 19) / 20);
    
    public async Task OnGetAsync()
    {
        
        var query = db.Set<Customer>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchTerm)) { var keyword = SearchTerm.Trim(); query = query.Where(x => x.FullName.Contains(keyword) || (x.Phone != null && x.Phone.Contains(keyword)) || (x.Email != null && x.Email.Contains(keyword))); }
        if (StatusFilter == "Active") query = query.Where(x => x.IsActive);
        else if (StatusFilter == "Inactive") query = query.Where(x => !x.IsActive);
        
        Total = await query.CountAsync(); PageNumber = Math.Clamp(PageNumber,1,TotalPages);
        Items = await query.OrderByDescending(x => x.CustomerId).Skip((PageNumber-1)*20).Take(20).ToListAsync();
    }
    public async Task<IActionResult> OnPostToggleStatusAsync(int id) {
            var item = await admin.FindAsync<Customer>(id); if (item == null) return NotFound();
            item.IsActive = !item.IsActive; await admin.SaveAsync(item,id); TempData["SuccessMessage"] = item.IsActive ? "Đã mở khóa khách hàng." : "Đã khóa khách hàng.";
            return RedirectToPage();
        }
}
