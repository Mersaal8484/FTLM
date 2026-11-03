using FuelTrack.Domain.MultiTenancy;
using FuelTrack.Platform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Admin.Pages;
public sealed class SubscriptionsModel(PlatformDbContext db) : PageModel
{
    [BindProperty] public Guid TenantId { get; set; }
    [BindProperty] public Guid PlanId { get; set; }
    [BindProperty] public DateOnly Start { get; set; }
    [BindProperty] public DateOnly End { get; set; }
    public string? Error { get; private set; }
    [TempData] public string? Success { get; set; }
    public List<PlatformTenant> Tenants { get; private set; } = [];
    public List<SubscriptionPlan> Plans { get; private set; } = [];
    public List<SubscriptionRow> Rows { get; private set; } = [];
    public async Task OnGetAsync()
    {
        Tenants = await db.Tenants.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        Plans = await db.Plans.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
        Rows = await (from s in db.Subscriptions.AsNoTracking() join t in db.Tenants on s.TenantId equals t.Id join p in db.Plans on s.PlanId equals p.Id orderby s.StartsAt descending select new SubscriptionRow(t.Name, p.Name, s.StartsAt, s.EndsAt)).ToListAsync();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!ModelState.IsValid || !await db.Tenants.AnyAsync(x => x.Id == TenantId) || !await db.Plans.AnyAsync(x => x.Id == PlanId && x.IsActive)) throw new ArgumentException();
            var start = new DateTimeOffset(Start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = new DateTimeOffset(End.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var subscription = new PlatformSubscription(Guid.NewGuid(), TenantId, PlanId, start, end);
            await using var transaction = await db.Database.BeginTransactionAsync();
            var lockKey = $"fueltrack:subscription:{TenantId:D}";
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))");
            if (await db.Subscriptions.AnyAsync(s => s.TenantId == TenantId && s.StartsAt < end && start < s.EndsAt))
                throw new ArgumentException("Overlapping subscription");
            db.Subscriptions.Add(subscription);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            Success = "تم تسجيل الاشتراك بنجاح.";
            return RedirectToPage();
        }
        catch (ArgumentException) { Error = "تحقق من المستأجر والباقة والتواريخ، وتأكد من عدم تداخل الاشتراكات."; }
        await OnGetAsync();
        return Page();
    }
    public sealed record SubscriptionRow(string Tenant, string Plan, DateTimeOffset Start, DateTimeOffset End)
    {
        public string State(DateTimeOffset now) => now < Start ? "upcoming" : now < End ? "active" : "expired";
    }
}
