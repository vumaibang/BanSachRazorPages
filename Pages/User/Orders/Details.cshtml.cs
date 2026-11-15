using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.User.Orders;
public class DetailsModel(OrderService orders) : StorePageModel
{
    public Order Order { get; set; } = null!;
    public async Task<IActionResult> OnGetAsync(int id) { var order = await orders.FindAsync(id, false); if (order == null) return NotFound(); Order = order; return Page(); }
    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        try { await orders.ChangeStatusAsync(id, "Cancelled", false); TempData["SuccessMessage"] = "Đã cập nhật trạng thái đơn hàng."; }
        catch (ValidationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToPage(new { id });
    }
}
