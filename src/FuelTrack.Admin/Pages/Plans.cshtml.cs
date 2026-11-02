using FuelTrack.Domain.MultiTenancy;
using FuelTrack.Platform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Admin.Pages;
public sealed class PlansModel(PlatformDbContext db) : PageModel
{
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public decimal Price { get; set; }
    [BindProperty] public string Currency { get; set; } = "";
    public string? Error { get; private set; }
    public List<SubscriptionPlan> Plans { get; private set; } = [];
    public async Task OnGetAsync() => Plans = await db.Plans.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Name) || Name.Length > 200 || Price > 9999999999999999m || decimal.Round(Price, 2) != Price) throw new ArgumentException();
            db.Plans.Add(new SubscriptionPlan(Guid.NewGuid(), Name, Price, Currency));
            await db.SaveChangesAsync();
            return RedirectToPage();
        }
        catch (ArgumentException) { Error = "تحقق من اسم الباقة والسعر ورمز العملة."; }
        await OnGetAsync();
        return Page();
    }
}
