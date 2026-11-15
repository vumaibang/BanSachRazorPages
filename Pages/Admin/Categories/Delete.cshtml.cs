using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin.Categories;
public class DeleteModel(AdminService admin) : StorePageModel
{
    public Category Item { get; set; } = null!;
    public async Task<IActionResult> OnGetAsync(int id) { var item = await admin.FindAsync<Category>(id); if (item == null) return NotFound(); Item = item; return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var result = await OnGetAsync(id); if (result is NotFoundResult) return result;
        if (!await TrySaveAsync(async () => TempData["SuccessMessage"] = await admin.DeleteAsync<Category>(id))) return Page();
        return RedirectToPage("./Index");
    }
}
