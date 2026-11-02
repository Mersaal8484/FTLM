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
    public string? Error { get; private set; }
    public List<PlatformTenant> Tenants { get; private set; } = [];
    public bool CanProvision => services.GetService<TenantProvisioningService>() is not null;
    public async Task OnGetAsync() => Tenants = await db.Tenants.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Name) || Name.Length > 200) throw new ArgumentException("Invalid name.");
            db.Tenants.Add(new PlatformTenant(Guid.NewGuid(), Name, Host));
            await db.SaveChangesAsync();
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
            await provisioner.ProvisionAsync(id, adminEmail, adminPassword, cancellationToken);
            return RedirectToPage();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or Npgsql.NpgsqlException or DbUpdateException or System.Security.Cryptography.CryptographicException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Tenant provisioning failed for {TenantId}", id);
            Error = "تعذر تجهيز المستأجر. تحقق من إعدادات المفتاح وحالة قاعدة البيانات.";
            await OnGetAsync();
            return Page();
        }
    }
}
