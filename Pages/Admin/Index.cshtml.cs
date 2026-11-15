using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.Admin;
public class IndexModel(AdminService admin) : PageModel
{
    public Dashboard Dashboard { get; set; } = null!;
    public async Task OnGetAsync() => Dashboard = await admin.DashboardAsync();
}
