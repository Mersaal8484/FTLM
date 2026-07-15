namespace FuelTrack.Application.Common;

public static class CacheKeys
{
    public const string LookupsAll = "lookups:all";
    public static string LookupsByCategory(string category) => $"lookups:category:{category}";
    public const string RolesAll = "roles:all";
    public static string RoleById(int id) => $"roles:{id}";
    public const string PermissionsAll = "permissions:all";
    public static string RolePermissions(int roleId) => $"roles:{roleId}:perms";
    public const string DashboardStats = "dashboard:stats";
    public const string InventorySummary = "dashboard:inventory";
    public static string FullDailyReport(DateTime from, DateTime to, int? subId) =>
        $"reports:daily:{from:yyyyMMdd}:{to:yyyyMMdd}:{subId ?? 0}";
    public static string PeriodicReport(DateTime from, DateTime to, int? subId) =>
        $"reports:periodic:{from:yyyyMMdd}:{to:yyyyMMdd}:{subId ?? 0}";
}
