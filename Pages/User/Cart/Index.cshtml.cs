using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.User.Cart;
public class IndexModel(CartService cart) : PageModel
{
    public List<CartItem> Items { get; set; } = [];
    public async Task<IActionResult> OnGetAsync() { if (User.IsInRole("Admin")) return RedirectToPage("/User/AccessDenied"); Items = await cart.GetAsync(); return Page(); }
    public async Task<IActionResult> OnPostAddAsync(int productId, int quantity = 1, string? returnUrl = null)
    {
        try { await cart.AddAsync(productId, quantity); TempData["CartMessage"] = "Đã thêm sách vào giỏ hàng."; }
        catch (ValidationException ex) { TempData["ErrorMessage"] = ex.Message; }
        if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl!);
        return RedirectToPage();
    }
    public IActionResult OnPostBuyNow(int productId, int quantity = 1) => RedirectToPage("/User/Cart/Checkout", new { ProductId = productId, Quantity = quantity });
    public async Task<IActionResult> OnPostUpdateAsync(int productId, int quantity)
    {
        try { if (quantity < 1) throw new ValidationException("Số lượng phải lớn hơn 0."); await cart.UpdateAsync(productId, quantity); TempData["CartMessage"] = "Đã cập nhật giỏ hàng."; }
        catch (ValidationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostRemoveAsync(int productId) { try { await cart.UpdateAsync(productId, 0); } catch (ValidationException ex) { TempData["ErrorMessage"] = ex.Message; } return RedirectToPage(); }
    public async Task<IActionResult> OnPostClearAsync() { try { await cart.ClearAsync(); } catch (ValidationException ex) { TempData["ErrorMessage"] = ex.Message; } return RedirectToPage(); }
}
