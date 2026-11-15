using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BanSachRazorPages.Data;
using BanSachRazorPages.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Services;

public sealed class RegistrationInput
{
    [Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(50)] public string Username { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 100 ký tự.")] public string Password { get; set; } = "";
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")] public string ConfirmPassword { get; set; } = "";
    [Required(ErrorMessage = "Nhập họ tên."), StringLength(100)] public string FullName { get; set; } = "";
    [StringLength(20)] public string? Phone { get; set; }
    [EmailAddress, StringLength(255)] public string? Email { get; set; }
    [StringLength(255)] public string? Address { get; set; }
}

public sealed class PasswordInput
{
    [Required] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 6)] public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")] public string ConfirmPassword { get; set; } = "";
}

public class AccountService(IDbContextFactory<AppDbContext> factory)
{
    private readonly PasswordHasher<Account> hasher = new();

    public async Task<Account?> LoginAsync(string username, string password)
    {
        await using var db = await factory.CreateDbContextAsync();
        var account = await db.Accounts.Include(a => a.Customer).FirstOrDefaultAsync(a => a.Username == username.Trim());
        if (account is null || string.IsNullOrEmpty(password) || password.Length > 100) return null;
        var result = hasher.VerifyHashedPassword(account, account.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (!account.IsActive || (account.Role == "Customer" && account.Customer?.IsActive != true))
            throw new ValidationException("Tài khoản đã bị khóa hoặc chưa có hồ sơ khách hàng.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = hasher.HashPassword(account, password);
            await db.SaveChangesAsync();
        }
        return account;
    }

    public async Task RegisterAsync(RegistrationInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (await db.Accounts.AnyAsync(a => a.Username == input.Username.Trim()))
            throw new ValidationException("Tên đăng nhập đã tồn tại.");
        var account = new Account { Username = input.Username.Trim(), Role = "Customer" };
        account.PasswordHash = hasher.HashPassword(account, input.Password);
        account.Customer = new Customer { FullName = input.FullName.Trim(), Phone = input.Phone?.Trim(), Email = input.Email?.Trim(), Address = input.Address?.Trim() };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public static string Stamp(Account account) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(account.PasswordHash)));
    public static bool Matches(Account? account, ClaimsPrincipal principal) => account is { IsActive: true }
        && account.Role == principal.FindFirstValue(ClaimTypes.Role)
        && Stamp(account) == principal.FindFirstValue("PasswordStamp")
        && (account.Role != "Customer" || account.Customer?.IsActive == true);
}

