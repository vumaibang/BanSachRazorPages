using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.User.Profile;
public class IndexModel(OrderService orders) : StorePageModel
{
    [BindProperty] public CustomerInput Input { get; set; } = new();
    public async Task OnGetAsync() => Input = CustomerInput.From(await orders.ProfileAsync());
    public async Task<IActionResult> OnPostAsync() { if (!await TrySaveAsync(() => orders.SaveProfileAsync(Input.ToEntity(0)))) return Page(); TempData["SuccessMessage"] = "Đã cập nhật thông tin cá nhân."; return RedirectToPage(); }
}
