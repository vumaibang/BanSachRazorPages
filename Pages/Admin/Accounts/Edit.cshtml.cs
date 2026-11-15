using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin.Accounts;
public class EditModel(AdminService admin) : StorePageModel
{
    public int Id { get; set; }
    [BindProperty] public AccountInput Input { get; set; } = new();
    
    public async Task<IActionResult> OnGetAsync(int id) {  var item = await admin.FindAsync<Account>(id); if (item == null) return NotFound(); Id = id; Input = new() { AccountId = id, Username = item.Username, Role = item.Role, IsActive = item.IsActive }; return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        Id = id; var existing = await admin.FindAsync<Account>(Id); if (existing == null) return NotFound();
        if (!await TrySaveAsync(async () => { Input.AccountId = Id; await admin.SaveAccountAsync(Input); })) return Page();
        TempData["SuccessMessage"] = "Đã lưu tài khoản.";
        return RedirectToPage("./Index");
    }
}
