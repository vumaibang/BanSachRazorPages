using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin;
public class LoginModel(AccountService accounts) : StorePageModel
{
    [BindProperty, Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(50)] public string Username { get; set; } = "";
    [BindProperty, Required(ErrorMessage = "Nhập mật khẩu."), StringLength(100)] public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? RedirectToPage(User.IsInRole("Admin") ? "/Admin/Index" : "/User/Index") : Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        try {
            var account = await accounts.LoginAsync(Username, Password);
            if (account?.Role != "Admin") throw new ValidationException("Tên đăng nhập hoặc mật khẩu không đúng cho khu vực này.");
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CurrentUser.Principal(account));
            // Giỏ khách được giữ để hợp nhất vào giỏ tài khoản khi mở giỏ/thanh toán.
            if (Url.IsLocalUrl(ReturnUrl)) return LocalRedirect(ReturnUrl!);
            return RedirectToPage("/Admin/Index");
        } catch (ValidationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return Page(); }
    }
}
