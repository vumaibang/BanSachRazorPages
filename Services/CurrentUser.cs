using System.Security.Claims;
using BanSachRazorPages.Data;
using BanSachRazorPages.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Services;

public class CurrentUser(IHttpContextAccessor accessor, IDbContextFactory<AppDbContext> factory)
{
    public async Task<Account?> OptionalAsync()
    {
        var principal = accessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return null;
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            throw new UnauthorizedAccessException("Vui lòng đăng nhập lại.");
        await using var db = await factory.CreateDbContextAsync();
        var account = await db.Accounts.AsNoTracking().Include(a => a.Customer).FirstOrDefaultAsync(a => a.AccountId == id);
        if (!AccountService.Matches(account, principal)) throw new UnauthorizedAccessException("Phiên đăng nhập đã thay đổi. Vui lòng đăng nhập lại.");
        return account;
    }
    public async Task<Account> RequireAsync(string role)
    {
        var account = await OptionalAsync();
        if (account?.Role != role) throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này.");
        return account;
    }
    public static ClaimsPrincipal Principal(Account account) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
        new Claim(ClaimTypes.Name, account.Username), new Claim(ClaimTypes.Role, account.Role),
        new Claim("PasswordStamp", AccountService.Stamp(account))
    }, CookieAuthenticationDefaults.AuthenticationScheme));
}

public class AccountCookieEvents(IDbContextFactory<AppDbContext> factory) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { context.RejectPrincipal(); return; }
        await using var db = await factory.CreateDbContextAsync();
        var account = await db.Accounts.AsNoTracking().Include(a => a.Customer).FirstOrDefaultAsync(a => a.AccountId == id);
        if (!AccountService.Matches(account, context.Principal!))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    }
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        var login = context.Request.Path.StartsWithSegments("/Admin") ? "/Admin/Login" : "/User/Login";
        context.Response.Redirect(login + "?ReturnUrl=" + Uri.EscapeDataString(context.Request.PathBase + context.Request.Path + context.Request.QueryString));
        return Task.CompletedTask;
    }
}
