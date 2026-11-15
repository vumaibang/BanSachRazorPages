using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin.Accounts;
public class IndexModel(BanSachRazorPages.Data.AppDbContext db, AdminService admin) : PageModel
{
    public List<Account> Items { get; set; } = [];
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int Total { get; set; }
    public int TotalPages => Math.Max(1, (Total + 19) / 20);
    [BindProperty(SupportsGet = true)] public string? RoleFilter { get; set; }
    public async Task OnGetAsync()
    {
        
        var query = db.Set<Account>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchTerm)) { var keyword = SearchTerm.Trim(); query = query.Where(x => x.Username.Contains(keyword)); }
        if (StatusFilter == "Active") query = query.Where(x => x.IsActive);
        else if (StatusFilter == "Inactive") query = query.Where(x => !x.IsActive);
        if (RoleFilter is "Admin" or "Customer") query = query.Where(x => x.Role == RoleFilter);
        Total = await query.CountAsync(); PageNumber = Math.Clamp(PageNumber,1,TotalPages);
        Items = await query.OrderByDescending(x => x.AccountId).Skip((PageNumber-1)*20).Take(20).ToListAsync();
    }
    public async Task<IActionResult> OnPostToggleStatusAsync(int id) {
            var item = await admin.FindAsync<Account>(id); if (item == null) return NotFound();
            try { await admin.SaveAccountAsync(new() { AccountId = id, Username = item.Username, Role = item.Role, IsActive = !item.IsActive }); TempData["SuccessMessage"] = item.IsActive ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản."; }
            catch (ValidationException ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToPage();
        }
}
