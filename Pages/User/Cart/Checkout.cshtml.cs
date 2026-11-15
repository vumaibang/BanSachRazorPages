using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.User.Cart;
public class CheckoutModel(OrderService orders, CartService cart, CatalogService catalog) : StorePageModel
{
    [BindProperty] public CheckoutInput Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int? ProductId { get; set; }
    [BindProperty(SupportsGet = true), Range(1, int.MaxValue)] public int Quantity { get; set; } = 1;
    public List<CartItem> Items { get; set; } = [];
    private async Task LoadAsync()
    {
        if (ProductId.HasValue) {
            var p = await catalog.ProductAsync(ProductId.Value) ?? throw new ValidationException("Sách không còn kinh doanh.");
            if (Quantity <= 0 || Quantity > p.StockQuantity) throw new ValidationException("Số lượng vượt quá tồn kho hoặc không hợp lệ.");
            Items = [new() { ProductId = p.ProductId, ProductName = p.ProductName, Price = p.Price, Quantity = Quantity, StockQuantity = p.StockQuantity }];
        } else Items = await cart.GetAsync();
    }
    public async Task<IActionResult> OnGetAsync()
    {
        try { var profile = await orders.ProfileAsync(); Input.ShippingAddress = profile.Address ?? ""; Input.Phone = profile.Phone ?? ""; await LoadAsync(); }
        catch (ValidationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try { await LoadAsync(); } catch (ValidationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        var id = 0;
        if (!await TrySaveAsync(async () => id = await orders.CheckoutAsync(Input, ProductId, Quantity))) return Page();
        TempData["OrderMessage"] = "Đặt hàng thành công! Nhà sách đã nhận đơn hàng của bạn.";
        return RedirectToPage("/User/Orders/Details", new { id });
    }
}
