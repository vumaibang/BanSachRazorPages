using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin;
public class LogoutModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/User/Index");
    public async Task<IActionResult> OnPostAsync() { await HttpContext.SignOutAsync(); HttpContext.Session.Clear(); return RedirectToPage("/User/Index"); }
}
