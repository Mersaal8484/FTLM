using FuelTrack.Domain.MultiTenancy;
using FuelTrack.Platform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FuelTrack.Admin.Services;

namespace FuelTrack.Admin.Pages;
public sealed class IndexModel(PlatformDbContext db, IServiceProvider services, ILogger<IndexModel> logger) : PageModel
{
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string Host { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public string? Error { get; private set; }
    [TempData] public string? Success { get; set; }
    public List<TenantRow> Tenants { get; private set; } = [];
    public int TotalTenants { get; private set; }
    public int ProvisioningCount { get; private set; }
    public int ActiveCount { get; private set; }
    public int ResultCount { get; private set; }
    public int PageCount => Math.Max(1, (ResultCount + 19) / 20);
    public bool CanProvision => services.GetService<TenantProvisioningService>() is not null;
    public async Task OnGetAsync()
    {
        Q = string.IsNullOrWhiteSpace(Q) ? null : Q.Trim();
        if (Q?.Length > 100) Q = Q[..100];
        var tenants = db.Tenants.AsNoTracking();
        TotalTenants = await tenants.CountAsync();
        ProvisioningCount = await tenants.CountAsync(t => t.Status == PlatformTenantStatus.Provisioning);
        ActiveCount = await tenants.CountAsync(t => t.Status == PlatformTenantStatus.Active);
        if (Q is not null)
        {
            var pattern = $"%{Q.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            tenants = tenants.Where(t => EF.Functions.ILike(t.Name, pattern, "\\") || EF.Functions.ILike(t.Host, pattern, "\\"));
        }
        ResultCount = await tenants.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);
        Tenants = await tenants.OrderBy(t => t.Name).ThenBy(t => t.Id)
            .Skip((PageNumber - 1) * 20).Take(20)
            .Select(t => new TenantRow(t.Id, t.Name, t.Host, t.Status, db.TenantCredentials.Any(c => c.TenantId == t.Id)))
            .ToListAsync();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Name) || Name.Length > 200) throw new ArgumentException("Invalid name.");
            db.Tenants.Add(new PlatformTenant(Guid.NewGuid(), Name, Host));
            await db.SaveChangesAsync();
            Success = "تمت إضافة المستأجر. يمكنك تجهيز قاعدة بياناته من القائمة.";
            return RedirectToPage();
        }
        catch (ArgumentException) { Error = "تحقق من الاسم والنطاق."; }
        catch (DbUpdateException) { Error = "تعذر الحفظ؛ تأكد من أن النطاق غير مستخدم."; }
        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostProvisionAsync(Guid id, string adminEmail, string adminPassword, CancellationToken cancellationToken)
    {
        var provisioner = services.GetService<TenantProvisioningService>();
        if (provisioner is null) return BadRequest("Tenant provisioning is not configured.");
        try
        {
            var created = await provisioner.ProvisionAsync(id, adminEmail, adminPassword, cancellationToken);
            Success = created ? "تم تجهيز قاعدة بيانات المستأجر ومديره. وصول العملاء غير مفعّل بعد." : "قاعدة بيانات هذا المستأجر مجهّزة مسبقاً.";
            return RedirectToPage();
        }
        catch (ArgumentException ex) when (ex.ParamName is "adminEmail" or "adminPassword")
        {
            Error = ex.ParamName == "adminEmail"
                ? "أدخل بريداً إلكترونياً صحيحاً لمدير المستأجر."
                : "يجب أن تتكون كلمة مرور المدير من 12 إلى 128 حرفاً، وتحتوي على حرف كبير وحرف صغير ورقم ورمز خاص.";
            await OnGetAsync();
            return Page();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or Npgsql.NpgsqlException or DbUpdateException or System.Security.Cryptography.CryptographicException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Tenant provisioning failed for {TenantId}", id);
            Error = "تعذر تجهيز المستأجر. تحقق من إعدادات المفتاح وحالة قاعدة البيانات.";
            await OnGetAsync();
            return Page();
        }
    }
    public sealed record TenantRow(Guid Id, string Name, string Host, PlatformTenantStatus Status, bool IsProvisioned);
}
