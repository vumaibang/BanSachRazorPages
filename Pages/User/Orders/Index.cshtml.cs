using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;
using BanSachRazorPages.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Pages.User.Orders;
public class IndexModel(OrderService orders) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public List<Order> Items { get; set; } = [];
    public int Total { get; set; }
    public int TotalPages => Math.Max(1,(Total+19)/20);
    public async Task OnGetAsync()
    {
        var items = await orders.ListAsync(false);
        var query = items.Where(o => (string.IsNullOrEmpty(Status) || o.Status == Status) && (string.IsNullOrWhiteSpace(SearchTerm) || $"{o.OrderId} {o.Customer.FullName} {o.Phone}".Contains(SearchTerm.Trim(),StringComparison.OrdinalIgnoreCase)));
        Total = query.Count(); PageNumber = Math.Clamp(PageNumber,1,TotalPages); Items = query.Skip((PageNumber-1)*20).Take(20).ToList();
    }
}
