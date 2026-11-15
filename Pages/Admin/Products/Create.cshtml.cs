using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin.Products;
public class CreateModel(AdminService admin) : StorePageModel
{
    public int Id { get; set; }
    [BindProperty] public ProductInput Input { get; set; } = new();
    public List<Category> Categories { get; set; } = []; [BindProperty] public IFormFile? ImageFile { get; set; } public string? ImageUrl { get; set; }
    public async Task<IActionResult> OnGetAsync(int id) { Categories = await admin.ListAsync<Category>();  return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        Id = 0;Categories = await admin.ListAsync<Category>();
        if (!await TrySaveAsync(async () => { if (ImageFile is { Length: > 0 }) ImageUrl = await admin.UploadCoverAsync(ImageFile); await admin.SaveAsync(Input.ToEntity(Id, ImageUrl), Id); })) return Page();
        TempData["SuccessMessage"] = "Đã lưu sách.";
        return RedirectToPage("./Index");
    }
}
