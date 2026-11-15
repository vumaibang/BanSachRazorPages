using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.User;
public class RegisterModel(AccountService accounts) : StorePageModel
{
    [BindProperty] public RegistrationInput Input { get; set; } = new();
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? RedirectToPage(User.IsInRole("Admin") ? "/Admin/Index" : "/User/Index") : Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TrySaveAsync(() => accounts.RegisterAsync(Input))) return Page();
        TempData["RegisterSuccess"] = "Đăng ký thành công. Hãy đăng nhập để mua sách.";
        return RedirectToPage("/User/Login");
    }
}
