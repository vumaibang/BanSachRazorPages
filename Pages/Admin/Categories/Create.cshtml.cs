using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin.Categories;
public class CreateModel(AdminService admin) : StorePageModel
{
    public int Id { get; set; }
    [BindProperty] public CategoryInput Input { get; set; } = new();
    
    public async Task<IActionResult> OnGetAsync(int id) {   return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        Id = 0;
        if (!await TrySaveAsync(async () => { await admin.SaveAsync(Input.ToEntity(Id), Id); })) return Page();
        TempData["SuccessMessage"] = "Đã lưu danh mục.";
        return RedirectToPage("./Index");
    }
}
