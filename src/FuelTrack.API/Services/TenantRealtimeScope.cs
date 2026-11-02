using FuelTrack.Application.Interfaces;

namespace FuelTrack.API.Services;

internal static class TenantRealtimeScope
{
    public static Guid? GetTenant(IConfiguration configuration, ITenantRequestContext? context)
    {
        if (!configuration.GetValue<bool>("MultiTenancy:Enabled")) return null;
        if (context?.TenantId is not Guid id || id == Guid.Empty)
            throw new InvalidOperationException("A trusted tenant context is required for realtime notifications.");
        return id;
    }
    public static string Group(Guid? tenantId, string name) => tenantId is Guid id ? $"tenant:{id:N}:group:{name}" : name;
    public static string All(Guid tenantId) => $"tenant:{tenantId:N}:all";
}
