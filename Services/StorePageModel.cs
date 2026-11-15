using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Services;

public abstract class StorePageModel : PageModel
{
    protected async Task<bool> TrySaveAsync(Func<Task> action)
    {
        if (!ModelState.IsValid) return false;
        try { await action(); return true; }
        catch (ValidationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        catch (DbUpdateException ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<StorePageModel>>().LogWarning(ex, "Không thể cập nhật dữ liệu");
            ModelState.AddModelError(string.Empty, "Dữ liệu đang được sử dụng hoặc vừa thay đổi. Vui lòng tải lại và thử lại.");
        }
        return false;
    }
}
