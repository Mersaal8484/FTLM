using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Data;

/// <summary>
/// Defines ALL pages/modules in the system.
/// This is the Single Source of Truth for permissions, menu items, and scope enforcement.
/// </summary>
public static class ModuleRegistrySeed
{
    /// <summary>
    /// Seeds all module definitions into the database.
    /// Safe to call on every startup — idempotent via Key lookup.
    /// </summary>
    public static async Task SeedAsync(FuelTrackDbContext db)
    {
        var modules = GetModules();
        var keys = modules.Select(m => m.Key).ToList();

        var existing = await db.ModuleDefinitions
            .Where(m => keys.Contains(m.Key))
            .ToListAsync();

        var existingMap = existing.ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var module in modules)
        {
            if (existingMap.TryGetValue(module.Key, out var found))
            {
                found.NameEn = module.NameEn;
                found.NameAr = module.NameAr;
                found.Route = module.Route;
                found.Icon = module.Icon;
                found.ParentKey = module.ParentKey;
                found.Template = module.Template;
                found.ScopeType = module.ScopeType;
                found.SortOrder = module.SortOrder;
                found.IsVisible = module.IsVisible;
                found.CustomActions = module.CustomActions;
                found.SupportsAuditLog = module.SupportsAuditLog;
                found.SupportsGlobalSearch = module.SupportsGlobalSearch;
            }
            else
            {
                db.ModuleDefinitions.Add(module);
            }
        }

        await db.SaveChangesAsync();
    }

    public static List<ModuleDefinition> GetModules()
    {
        return new List<ModuleDefinition>
        {
            // ============================================
            // ADMINISTRATION
            // ============================================
            new ModuleDefinition
            {
                Key = "users",
                NameEn = "Users",
                NameAr = "المستخدمين",
                Route = "/users",
                Icon = "Users",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.None,
                SortOrder = 100,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "roles",
                NameEn = "Roles",
                NameAr = "الأدوار",
                Route = "/roles",
                Icon = "Shield",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.None,
                SortOrder = 101,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = false
            },
            new ModuleDefinition
            {
                Key = "branches",
                NameEn = "Branches",
                NameAr = "الفروع",
                Route = "/locations/branches",
                Icon = "MapPin",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.None,
                SortOrder = 102,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "stations",
                NameEn = "Stations",
                NameAr = "المحطات",
                Route = "/locations/stations",
                Icon = "Building",
                ParentKey = "branches",
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.Branch,
                SortOrder = 103,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "sub_stations",
                NameEn = "Sub-Stations",
                NameAr = "المحطات الفرعية",
                Route = "/locations/sub-stations",
                Icon = "Home",
                ParentKey = "stations",
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.Station,
                SortOrder = 104,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "generators",
                NameEn = "Generators",
                NameAr = "المولدات",
                Route = "/generators",
                Icon = "Zap",
                ParentKey = "sub_stations",
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.SubStation,
                SortOrder = 200,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "fuel_tanks",
                NameEn = "Fuel Tanks",
                NameAr = "خزانات الوقود",
                Route = "/tanks/fuel",
                Icon = "Droplets",
                ParentKey = "sub_stations",
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.SubStation,
                SortOrder = 201,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "oil_tanks",
                NameEn = "Oil Tanks",
                NameAr = "خزانات الزيت",
                Route = "/tanks/oil",
                Icon = "Droplet",
                ParentKey = "sub_stations",
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.SubStation,
                SortOrder = 202,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "meters",
                NameEn = "Meters",
                NameAr = "العدادات",
                Route = "/meters",
                Icon = "Gauge",
                ParentKey = "sub_stations",
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.SubStation,
                SortOrder = 203,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "controllers",
                NameEn = "Controllers",
                NameAr = "الوحدات контроллر",
                Route = "/controllers",
                Icon = "Cpu",
                ParentKey = "sub_stations",
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.SubStation,
                SortOrder = 204,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "lookups",
                NameEn = "Lookups",
                NameAr = "القوائم المرجعية",
                Route = "/lookups",
                Icon = "List",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.None,
                SortOrder = 105,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },

            // ============================================
            // OPERATIONS
            // ============================================
            new ModuleDefinition
            {
                Key = "daily_readings",
                NameEn = "Daily Readings",
                NameAr = "القراءات اليومية",
                Route = "/readings",
                Icon = "BookOpen",
                ParentKey = null,
                Template = ModuleTemplate.Approval,
                ScopeType = ScopeType.SubStation,
                SortOrder = 300,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true,
                CustomActions = "approve,reject"
            },
            new ModuleDefinition
            {
                Key = "station_readings",
                NameEn = "Station Readings",
                NameAr = "قراءات المحطة",
                Route = "/station-readings",
                Icon = "BarChart",
                ParentKey = null,
                Template = ModuleTemplate.Approval,
                ScopeType = ScopeType.SubStation,
                SortOrder = 301,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true,
                CustomActions = "approve,reject"
            },
            new ModuleDefinition
            {
                Key = "operational_data",
                NameEn = "Operational Data",
                NameAr = "البيانات التشغيلية",
                Route = "/operations",
                Icon = "Zap",
                ParentKey = null,
                Template = ModuleTemplate.ReadOnly,
                ScopeType = ScopeType.SubStation,
                SortOrder = 308,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = false,
                CustomActions = "capture,review,configure,export,flag_meter_reset"
            },
            new ModuleDefinition
            {
                Key = "fuel_movements",
                NameEn = "Fuel Movements",
                NameAr = "حركة الوقود",
                Route = "/fuel-movements",
                Icon = "Truck",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.SubStation,
                SortOrder = 302,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true,
                CustomActions = "send,receive,approve,reject"
            },
            new ModuleDefinition
            {
                Key = "oil_transactions",
                NameEn = "Oil Transactions",
                NameAr = "حركات الزيت",
                Route = "/oil-transactions",
                Icon = "Droplet",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.SubStation,
                SortOrder = 303,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true,
                CustomActions = "send,receive,approve,reject"
            },
            new ModuleDefinition
            {
                Key = "periodic_inventory",
                NameEn = "Periodic Inventory",
                NameAr = "الجرد الدوري",
                Route = "/inventory",
                Icon = "ClipboardList",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.SubStation,
                SortOrder = 304,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true,
                CustomActions = "approve,reject"
            },
            new ModuleDefinition
            {
                Key = "notifications",
                NameEn = "Notifications",
                NameAr = "الإشعارات",
                Route = "/notifications",
                Icon = "Bell",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.None,
                SortOrder = 305,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },
            new ModuleDefinition
            {
                Key = "rollback_approval",
                NameEn = "Rollback Approval",
                NameAr = "موافقة التراجع",
                Route = "/rollback-approval",
                Icon = "RotateCcw",
                ParentKey = null,
                Template = ModuleTemplate.Approval,
                ScopeType = ScopeType.SubStation,
                SortOrder = 306,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = false,
                CustomActions = "approve,reject"
            },
            new ModuleDefinition
            {
                Key = "review_changes",
                NameEn = "Review Changes",
                NameAr = "مراجعة التغييرات",
                Route = "/review-changes",
                Icon = "Eye",
                ParentKey = null,
                Template = ModuleTemplate.ReadOnly,
                ScopeType = ScopeType.SubStation,
                SortOrder = 307,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },

            // ============================================
            // REPORTS
            // ============================================
            new ModuleDefinition
            {
                Key = "reports",
                NameEn = "Reports",
                NameAr = "التقارير",
                Route = "/reports",
                Icon = "FileText",
                ParentKey = null,
                Template = ModuleTemplate.Report,
                ScopeType = ScopeType.Station,
                SortOrder = 106,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },
            new ModuleDefinition
            {
                Key = "fuel_report",
                NameEn = "Fuel Report",
                NameAr = "تقرير الوقود",
                Route = "/reports/fuel",
                Icon = "FileText",
                ParentKey = "reports",
                Template = ModuleTemplate.Report,
                ScopeType = ScopeType.Station,
                SortOrder = 400,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },
            new ModuleDefinition
            {
                Key = "oil_report",
                NameEn = "Oil Report",
                NameAr = "تقرير الزيت",
                Route = "/reports/oil",
                Icon = "FileText",
                ParentKey = "reports",
                Template = ModuleTemplate.Report,
                ScopeType = ScopeType.Station,
                SortOrder = 401,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },
            new ModuleDefinition
            {
                Key = "efficiency_report",
                NameEn = "Efficiency Report",
                NameAr = "تقرير الكفاءة",
                Route = "/reports/efficiency",
                Icon = "FileText",
                ParentKey = "reports",
                Template = ModuleTemplate.Report,
                ScopeType = ScopeType.Station,
                SortOrder = 402,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },
            new ModuleDefinition
            {
                Key = "consumption_report",
                NameEn = "Consumption Report",
                NameAr = "تقرير الاستهلاك",
                Route = "/reports/consumption",
                Icon = "FileText",
                ParentKey = "reports",
                Template = ModuleTemplate.Report,
                ScopeType = ScopeType.Station,
                SortOrder = 403,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },
            new ModuleDefinition
            {
                Key = "inventory_report",
                NameEn = "Inventory Report",
                NameAr = "تقرير الجرد",
                Route = "/reports/inventory",
                Icon = "FileText",
                ParentKey = "reports",
                Template = ModuleTemplate.Report,
                ScopeType = ScopeType.Station,
                SortOrder = 404,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },

            // ============================================
            // DASHBOARD
            // ============================================
            new ModuleDefinition
            {
                Key = "dashboard",
                NameEn = "Dashboard",
                NameAr = "لوحة التحكم",
                Route = "/",
                Icon = "LayoutDashboard",
                ParentKey = null,
                Template = ModuleTemplate.Dashboard,
                ScopeType = ScopeType.None,
                SortOrder = 1,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },

            // ============================================
            // AUDIT
            // ============================================
            new ModuleDefinition
            {
                Key = "audit_logs",
                NameEn = "Audit Logs",
                NameAr = "سجلات التدقيق",
                Route = "/audit-logs",
                Icon = "FileSearch",
                ParentKey = null,
                Template = ModuleTemplate.Report,
                ScopeType = ScopeType.None,
                SortOrder = 500,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },

            // ============================================
            // SETTINGS
            // ============================================
            new ModuleDefinition
            {
                Key = "settings",
                NameEn = "Settings",
                NameAr = "الإعدادات",
                Route = "/settings",
                Icon = "Settings",
                ParentKey = null,
                Template = ModuleTemplate.Settings,
                ScopeType = ScopeType.None,
                SortOrder = 600,
                IsVisible = true,
                SupportsAuditLog = false,
                SupportsGlobalSearch = false
            },

            // ============================================
            // WAREHOUSE
            // ============================================
            new ModuleDefinition
            {
                Key = "warehouse",
                NameEn = "Warehouse",
                NameAr = "المخازن",
                Route = "/warehouse/parts",
                Icon = "Warehouse",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.Station,
                SortOrder = 700,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true
            },
            new ModuleDefinition
            {
                Key = "inventory",
                NameEn = "Inventory",
                NameAr = "المخزون",
                Route = "/warehouse/stock",
                Icon = "Boxes",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.Station,
                SortOrder = 702,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true,
                CustomActions = "approve_issue"
            },

            // ============================================
            // MAINTENANCE
            // ============================================
            new ModuleDefinition
            {
                Key = "maintenance",
                NameEn = "Maintenance",
                NameAr = "الصيانة",
                Route = "/maintenance",
                Icon = "Wrench",
                ParentKey = null,
                Template = ModuleTemplate.CRUD,
                ScopeType = ScopeType.Station,
                SortOrder = 701,
                IsVisible = true,
                SupportsAuditLog = true,
                SupportsGlobalSearch = true,
                CustomActions = "close_permit"
            },
            new ModuleDefinition
            {
                Key = "solar",
                NameEn = "Solar",
                NameAr = "الطاقة الشمسية",
                Route = "/solar",
                Icon = "Sun",
                ParentKey = null,
                Template = ModuleTemplate.ReadOnly,
                ScopeType = ScopeType.Station,
                SortOrder = 703,
                IsVisible = false,
                SupportsAuditLog = true,
                SupportsGlobalSearch = false
            },
        };
    }
}
