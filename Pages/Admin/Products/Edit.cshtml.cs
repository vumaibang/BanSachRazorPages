using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin.Products;
public class EditModel(AdminService admin) : StorePageModel
{
    public int Id { get; set; }
    [BindProperty] public ProductInput Input { get; set; } = new();
    public List<Category> Categories { get; set; } = []; [BindProperty] public IFormFile? ImageFile { get; set; } public string? ImageUrl { get; set; }
    public async Task<IActionResult> OnGetAsync(int id) { Categories = await admin.ListAsync<Category>(); var item = await admin.FindAsync<Product>(id); if (item == null) return NotFound(); Id = id; Input = ProductInput.From(item); ImageUrl = item.ImageUrl; return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        Id = id;Categories = await admin.ListAsync<Category>(); var existing = await admin.FindAsync<Product>(Id); if (existing == null) return NotFound(); ImageUrl = existing.ImageUrl;
        if (!await TrySaveAsync(async () => { if (ImageFile is { Length: > 0 }) ImageUrl = await admin.UploadCoverAsync(ImageFile); await admin.SaveAsync(Input.ToEntity(Id, ImageUrl), Id); })) return Page();
        TempData["SuccessMessage"] = "Đã lưu sách.";
        return RedirectToPage("./Index");
    }
}
