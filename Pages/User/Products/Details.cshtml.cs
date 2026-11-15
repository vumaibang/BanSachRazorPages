using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.User.Products;
public class DetailsModel(CatalogService catalog) : PageModel
{
    public Product Product { get; set; } = null!;
    public async Task<IActionResult> OnGetAsync(int id) { var product = await catalog.ProductAsync(id); if (product == null) return NotFound(); Product = product; return Page(); }
}
