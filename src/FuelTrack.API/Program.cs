using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using FuelTrack.API.Hubs;
using FuelTrack.API.Jobs;
using FuelTrack.API.Middleware;
using FuelTrack.API.Services;
using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddSingleton<FuelTrackMetrics>();

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<FuelTrack.API.Filters.UserScopeFilterAttribute>();
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });

builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
builder.Services.AddSignalR();

var jwtKey = builder.Configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JwtSettings:Key not configured");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "FuelTrack.API";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "FuelTrack.Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
})
.AddJwtBearer("SignalR", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = builder.Configuration["JwtSettings:SignalRAudience"] ?? jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/fueltrack"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FuelTrack API", Version = "v1" });
    c.SwaggerDoc("v2", new OpenApiInfo { Title = "FuelTrack API", Version = "v2" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<FuelTrackDbContext>(name: "database", tags: ["ready"])
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "", name: "redis", tags: ["ready", "infrastructure"]);

builder.Services.AddResponseCompression();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    options.AddSlidingWindowLimiter("ApiRead", limiterOptions =>
    {
        limiterOptions.PermitLimit = 60;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 6;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    options.AddSlidingWindowLimiter("ApiWrite", limiterOptions =>
    {
        limiterOptions.PermitLimit = 60;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 6;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    options.AddSlidingWindowLimiter("Auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 2;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.AddTokenBucketLimiter("login", limiterOptions =>
    {
        limiterOptions.TokenLimit = 5;
        limiterOptions.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        limiterOptions.TokensPerPeriod = 5;
        limiterOptions.AutoReplenishment = true;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterTime)
            ? retryAfterTime.TotalSeconds
            : 60;
        context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString("F0");
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded",
            retryAfter = (int)retryAfter
        }, cancellationToken);
    };
});

if (!builder.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            SchemaName = "Hangfire"
        }));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
        options.Queues = ["critical", "default", "low"];
    });
}

var otelEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
if (!string.IsNullOrEmpty(otelEndpoint))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService("FuelTrack.API", serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource("FuelTrack.API")
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("FuelTrack.API")
            .AddPrometheusExporter());
}

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ApiHardeningMiddleware>();
app.UseMiddleware<TokenCookieToHeaderMiddleware>();

app.UseCors("AllowFrontend");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FuelTrack API v1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "FuelTrack API v2");
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PermissionEnforcementMiddleware>();

app.UseMiddleware<InputSanitizationMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();

if (app.Services.GetService(typeof(Hangfire.IRecurringJobManager)) != null)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "FuelTrack Jobs",
        Authorization = new[] { new FuelTrack.API.Services.HangfireAllowAdminFilter() }
    });
}

app.MapControllers();
app.MapHub<FuelTrackHub>("/hubs/fueltrack");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description
            }),
            duration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapHealthChecks("/health");

if (!string.IsNullOrEmpty(otelEndpoint) && app.Services.GetService<OpenTelemetry.Metrics.MeterProvider>() != null)
{
    app.MapPrometheusScrapingEndpoint("/metrics");
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

    // SQL cold-start warm-up with retry (skip for non-relational providers like InMemory)
    if (dbContext.Database.IsRelational())
    {
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                await dbContext.Database.OpenConnectionAsync();
                await dbContext.Database.CloseConnectionAsync();
                Console.WriteLine($"[Startup] SQL warm-up OK on attempt {attempt}");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Startup] SQL warm-up attempt {attempt} failed: {ex.Message}");
                if (attempt == 5) throw;
                await Task.Delay(5000 * attempt);
            }

            // Increase EF Core command timeout for cold SQL Server startup
            dbContext.Database.SetCommandTimeout(120);
        }
    }

    // Seed module registry and generate permissions from it
    await FuelTrack.Infrastructure.Data.ModuleRegistrySeed.SeedAsync(dbContext);
    var permGen = new FuelTrack.Infrastructure.Data.PermissionGenerator(dbContext);
    await permGen.GenerateAllPermissionsAsync();

    // Seed roles and admin user
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleService = new FuelTrack.Infrastructure.Services.RoleService(roleManager, userManager, dbContext, scope.ServiceProvider.GetRequiredService<ICacheService>());
    await roleService.InitAsync();

    // Ensure admin@ft.com exists with Admin role and full scope
    const string adminEmail = "admin@ft.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var createResult = await userManager.CreateAsync(adminUser, "Admin@123");
        if (createResult.Succeeded)
        {
            adminUser.CreatedBy = adminUser.Id;
            await userManager.UpdateAsync(adminUser);
            await userManager.AddToRoleAsync(adminUser, "Admin");

            // Full scope = all null IDs = Admin (All)
            dbContext.UserScopes.Add(new UserScope
            {
                UserId = adminUser.Id
            });
            await dbContext.SaveChangesAsync();
        }
    }
    else
    {
        // Ensure existing admin has Admin role
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            await userManager.AddToRoleAsync(adminUser, "Admin");

        // Ensure admin has full scope
        var hasScope = await dbContext.UserScopes.AnyAsync(s => s.UserId == adminUser.Id);
        if (!hasScope)
        {
            dbContext.UserScopes.Add(new UserScope
            {
                UserId = adminUser.Id
            });
            await dbContext.SaveChangesAsync();
        }
    }

    var recurringJobManager = scope.ServiceProvider.GetService<IRecurringJobManager>();
    if (recurringJobManager != null)
    {
        recurringJobManager.AddOrUpdate<CleanupExpiredRefreshTokensJob>(
            "cleanup-refresh-tokens",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily);

        recurringJobManager.AddOrUpdate<CleanupOldAuditLogsJob>(
            "cleanup-old-audit-logs",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Weekly);

        recurringJobManager.AddOrUpdate<DatabaseMaintenanceJob>(
            "database-maintenance",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Monthly);

    recurringJobManager.AddOrUpdate<DatabaseBackupJob>(
        "database-backup",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 2 * * *");

    recurringJobManager.AddOrUpdate<FuelStockAlertJob>(
        "fuel-stock-alerts",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily);

    recurringJobManager.AddOrUpdate<GenerateDailyAnalyticsInsightsJob>(
        "daily-analytics-insights",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily);

    recurringJobManager.AddOrUpdate<WeeklyReportJob>(
        "weekly-report",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 8 * * 1");
    }
}

app.Run();
