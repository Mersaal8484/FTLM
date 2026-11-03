using FuelTrack.Application.Interfaces;
using FuelTrack.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Controllers;

[ApiController]
[Route("api/tenant-branding")]
public sealed class TenantBrandingController(IServiceProvider services, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!configuration.GetValue<bool>("MultiTenancy:Enabled")) return Ok(new { name = "FuelTrack", logoUrl = (string?)null });
        var id = services.GetRequiredService<ITenantRequestContext>().TenantId;
        if (id is null) return NotFound();
        var branding = await services.GetRequiredService<PlatformDbContext>().Tenants.AsNoTracking()
            .Where(t => t.Id == id).Select(t => new { t.DisplayName, HasLogo = t.Logo != null }).SingleOrDefaultAsync(cancellationToken);
        if (branding is null) return NotFound();
        return Ok(new { name = branding.DisplayName, logoUrl = branding.HasLogo ? "/api/tenant-branding/logo" : null });
    }

    [AllowAnonymous]
    [HttpGet("logo")]
    public async Task<IActionResult> Logo(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        if (!configuration.GetValue<bool>("MultiTenancy:Enabled")) return NotFound();
        var id = services.GetRequiredService<ITenantRequestContext>().TenantId;
        if (id is null) return NotFound();
        var branding = await services.GetRequiredService<PlatformDbContext>().Tenants.AsNoTracking()
            .Where(t => t.Id == id).Select(t => new { t.Logo, t.LogoContentType }).SingleOrDefaultAsync(cancellationToken);
        if (branding?.Logo is null || branding.LogoContentType is null) return NotFound();
        return File(branding.Logo, branding.LogoContentType);
    }
}
