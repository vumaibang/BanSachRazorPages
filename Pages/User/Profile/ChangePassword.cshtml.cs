using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.User.Profile;
public class ChangePasswordModel(OrderService orders) : StorePageModel
{
    [BindProperty] public PasswordInput Input { get; set; } = new();
    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.CurrentPassword == Input.NewPassword) ModelState.AddModelError(string.Empty, "Mật khẩu mới phải khác mật khẩu hiện tại.");
        if (!await TrySaveAsync(() => orders.ChangePasswordAsync(Input))) return Page();
        await HttpContext.SignOutAsync(); TempData["SuccessMessage"] = "Đã đổi mật khẩu. Vui lòng đăng nhập lại."; return RedirectToPage("/User/Login");
    }
}
