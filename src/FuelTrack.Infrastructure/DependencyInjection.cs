using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure.Migrations;
using FuelTrack.Infrastructure.Services;
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
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    sqlOptions.CommandTimeout(30);
                });

            options.AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>());
        });

        services.AddIdentity<FuelTrack.Domain.Models.User, FuelTrack.Domain.Models.Role>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
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
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();
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
        services.AddSingleton<ICacheService, HybridCacheService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddSingleton<ISecretsValidator, StartupSecretsValidator>();
        services.AddScoped<ArchiveQueryService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();

        // Migration lock
        services.AddScoped<MigrationLockService>();
        services.AddScoped<MigrationRunner>();

        return services;
    }
}
