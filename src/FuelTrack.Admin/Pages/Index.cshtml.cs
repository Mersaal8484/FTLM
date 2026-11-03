using FuelTrack.Domain.MultiTenancy;
using FuelTrack.Platform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FuelTrack.Admin.Services;

namespace FuelTrack.Admin.Pages;
[RequestSizeLimit(1_200_000)]
[RequestFormLimits(MultipartBodyLengthLimit = 1_100_000)]
public sealed class IndexModel(PlatformDbContext db, IServiceProvider services, ILogger<IndexModel> logger, IConfiguration configuration) : PageModel
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
    public bool CanResume => configuration.GetValue<bool>("Platform:CustomerAccessReady");
    public bool CanActivateLocalDemo => HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() && configuration.GetValue<bool>("Platform:LocalDemoEnabled");
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
            .Select(t => new TenantRow(t.Id, t.Name, t.DisplayName, t.Host, t.Status, db.TenantCredentials.Any(c => c.TenantId == t.Id), t.Logo != null))
            .ToListAsync();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Name) || Name.Length > 200) throw new ArgumentException("Invalid name.");
            var host = Host?.Trim() ?? "";
            if (CanActivateLocalDemo && !host.Contains('.')) host += ".localhost";
            if (CanActivateLocalDemo && (!host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) || host[..^".localhost".Length].Contains('.')))
                throw new ArgumentException("A local tenant needs a single subdomain.");
            if (CanActivateLocalDemo)
            {
                var label = host[..^".localhost".Length];
                if (label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-' ||
                    label.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
                    throw new ArgumentException("Invalid local tenant subdomain.");
            }
            db.Tenants.Add(new PlatformTenant(Guid.NewGuid(), Name, host));
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
    public async Task<IActionResult> OnPostSuspendAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await services.GetRequiredService<TenantLifecycleService>().SuspendAsync(id, cancellationToken);
            Success = "تم تعليق المستأجر؛ تُرفض طلباته الجديدة. افصل الاتصالات المفتوحة قبل الصيانة.";
            return RedirectToPage();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or DbUpdateException)
        {
            Error = "تعذر تعليق المستأجر. تحقق من حالته وحالة قاعدة البيانات.";
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostResumeAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await services.GetRequiredService<TenantLifecycleService>().ResumeAsync(id, CanResume, cancellationToken);
            Success = "تم استئناف المستأجر.";
            return RedirectToPage();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or DbUpdateException)
        {
            Error = "تعذر الاستئناف؛ يلزم اكتمال جاهزية الخدمة وتجهيز المستأجر ووجود اشتراك فعال.";
            await OnGetAsync();
            return Page();
        }
    }
    public async Task<IActionResult> OnPostActivateDemoAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await services.GetRequiredService<TenantLifecycleService>().ActivateLocalDemoAsync(id, CanActivateLocalDemo, cancellationToken);
            Success = "تم تفعيل النسخة التجريبية محلياً.";
            return RedirectToPage();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or DbUpdateException)
        {
            Error = "تعذر تفعيل النسخة التجريبية؛ تحقق من تجهيزها واشتراكها.";
            await OnGetAsync();
            return Page();
        }
    }
    public async Task<IActionResult> OnPostBrandingAsync(Guid id, string displayName, IFormFile? logo, bool removeLogo, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
                ?? throw new ArgumentException("Tenant not found.");
            byte[]? bytes = null;
            string? type = null;
            if (logo is { Length: > 0 })
            {
                if (logo.Length > 1_048_576 || removeLogo) throw new ArgumentException("Invalid logo size or action.");
                await using var buffer = new MemoryStream();
                await logo.CopyToAsync(buffer, cancellationToken);
                bytes = buffer.ToArray();
                if (bytes.Length != logo.Length) throw new ArgumentException("Invalid logo.");
                if (bytes.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })) type = "image/png";
                else if (bytes.AsSpan().StartsWith(new byte[] { 0xff, 0xd8, 0xff })) type = "image/jpeg";
                else if (bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)) type = "image/webp";
                else throw new ArgumentException("Unsupported logo format.");
            }
            tenant.UpdateBranding(displayName, bytes, type, removeLogo);
            await db.SaveChangesAsync(cancellationToken);
            Success = "تم حفظ الاسم الظاهر والشعار.";
            return RedirectToPage();
        }
        catch (Exception ex) when (ex is ArgumentException or DbUpdateException)
        {
            Error = "تعذر حفظ الهوية. أدخل اسماً صحيحاً وصورة PNG أو JPEG أو WebP بحجم لا يتجاوز 1 ميجابايت.";
            await OnGetAsync();
            return Page();
        }
    }
    public async Task<IActionResult> OnGetLogoAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.AsNoTracking().Where(t => t.Id == id)
            .Select(t => new { t.Logo, t.LogoContentType }).SingleOrDefaultAsync(cancellationToken);
        if (tenant?.Logo is null || tenant.LogoContentType is null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(tenant.Logo, tenant.LogoContentType);
    }
    public sealed record TenantRow(Guid Id, string Name, string DisplayName, string Host, PlatformTenantStatus Status, bool IsProvisioned, bool HasLogo);
}
