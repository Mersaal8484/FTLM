using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure.Migrations;
using FuelTrack.Infrastructure.Services;
using FuelTrack.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FuelTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SlowQueryInterceptor>();

        services.AddDbContext<FuelTrackDbContext>((sp, options) =>
        {
            var connStr = configuration.GetConnectionString("DefaultConnection") ?? "";
            if (configuration.GetValue<bool>("MultiTenancy:Enabled"))
            {
                var tenant = sp.GetService<ITenantRequestContext>();
                if (tenant?.TenantId is null || string.IsNullOrWhiteSpace(tenant.DatabaseConnection))
                    throw new InvalidOperationException("A trusted tenant database context is required.");
                options.UseNpgsql(tenant.DatabaseConnection);
            }
            else if (connStr.Contains(".db", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connStr);
            }
            else if (configuration["Database:Provider"]?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
            {
                options.UseSqlServer(connStr, sqlServer =>
                {
                    sqlServer.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    sqlServer.CommandTimeout(120);
                });
            }
            else
            {
                options.UseNpgsql(connStr, postgres =>
                {
                    postgres.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    postgres.CommandTimeout(120);
                });
            }

            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            options.AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>());
        });

        services.AddIdentity<FuelTrack.Domain.Models.User, FuelTrack.Domain.Models.Role>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyz" +
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "0123456789" +
                "-._@+" +
                "\u0600-\u06FF"; // Arabic Unicode block
        })
        .AddEntityFrameworkStores<FuelTrackDbContext>()
        .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();

        // Services
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IStationService, StationService>();
        services.AddScoped<ISubStationService, SubStationService>();
        services.AddScoped<IGeneratorService, GeneratorService>();
        services.AddScoped<IFuelTankService, FuelTankService>();
        services.AddScoped<IReadingService, ReadingService>();
        services.AddScoped<ISubStationReadingService, SubStationReadingService>();
        services.AddScoped<IFuelMovementService, FuelMovementService>();
        services.AddScoped<IOilService, OilService>();
        services.AddScoped<IOilTransactionService, OilTransactionService>();
        services.AddScoped<IPeriodicInventoryService, PeriodicInventoryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IStockSummaryService, StockSummaryService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IWorkflowNotificationService, WorkflowNotificationService>();
        services.AddScoped<IAlertGeneratorService, AlertGeneratorService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAnalyticsInsightService, AnalyticsInsightService>();
        services.AddScoped<IEfficiencyService, EfficiencyService>();
        services.AddScoped<IControllerService, ControllerService>();
        services.AddScoped<IMeterService, MeterService>();
        services.AddScoped<IScreenService, ScreenService>();
        services.AddScoped<IRollbackService, RollbackService>();
        services.AddScoped<IUserScopeFilterService, UserScopeFilterService>();
        services.AddDistributedMemoryCache();
        services.AddMemoryCache();
        services.AddSingleton<HybridCacheService>();
        services.AddScoped<ICacheService, TenantAwareCacheService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddSingleton<ISecretsValidator, StartupSecretsValidator>();
        services.AddScoped<ArchiveQueryService>();
        services.AddScoped<IDataArchivalService, DataArchivalService>();
        services.AddScoped<IComplianceService, ComplianceService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IStationOperationalSettingsService, StationOperationalSettingsService>();
        services.AddScoped<ICustomerFeatureService, CustomerFeatureService>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.Configure<AttachmentStorageOptions>(configuration.GetSection(AttachmentStorageOptions.SectionName));
        services.AddSingleton<IAttachmentStorage, PrivateFileAttachmentStorage>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IOperationalCaptureService, OperationalCaptureService>();
        services.AddScoped<ITankCalibrationService, TankCalibrationService>();
        services.AddScoped<IFeederService, FeederService>();
        services.AddScoped<IFuelLoadSafetyService, FuelLoadSafetyService>();
        services.AddScoped<OperationalCompletenessService>();
        services.AddScoped<IServiceItemService, ServiceItemService>();
        services.AddScoped<IServiceIntervalDefinitionService, ServiceIntervalDefinitionService>();
        services.AddScoped<IServiceReplacementService, ServiceReplacementService>();
        services.AddScoped<IServiceDueService, ServiceDueService>();
        services.AddScoped<IServiceDueEvaluationService, ServiceDueEvaluationService>();

        // Migration lock
        services.AddScoped<MigrationLockService>();
        services.AddScoped<MigrationRunner>();

        return services;
    }
}
