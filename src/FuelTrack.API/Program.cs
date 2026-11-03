using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using FuelTrack.API.HealthChecks;
using FuelTrack.API.Hubs;
using FuelTrack.API.Jobs;
using FuelTrack.API.Middleware;
using FuelTrack.API.Services;
using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Data;
using FuelTrack.Infrastructure.Services;
using FuelTrack.Platform;
using FuelTrack.API.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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
    // Hosting diagnostics include the raw query string, including SignalR bearer tokens.
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Fail closed until request-to-tenant identity, secret-backed tenant connections,
// tenant-scoped SignalR and background jobs are integrated and verified together.
var tenantProofMode = builder.Environment.IsEnvironment("Testing") &&
    builder.Configuration.GetValue<bool>("MultiTenancy:DisposableProof");
if (tenantProofMode)
{
    var testUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    var testDatabase = new Npgsql.NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Platform") ?? "");
    if (!Uri.TryCreate(testUrl, UriKind.Absolute, out var bindAddress) ||
        !bindAddress.IsLoopback || !string.Equals(testDatabase.Database, "model_proof", StringComparison.Ordinal))
        throw new InvalidOperationException("Tenant proof mode requires a loopback listener and the disposable model_proof database.");
}
if (builder.Configuration.GetValue<bool>("MultiTenancy:Enabled") && !tenantProofMode)
    throw new InvalidOperationException("Multi-tenant API access is not enabled until tenant isolation is fully integrated.");

builder.Host.UseSerilog();

if (builder.Configuration.GetValue<bool>("MultiTenancy:Enabled"))
{
    var platformConnection = builder.Configuration.GetConnectionString("Platform")
        ?? throw new InvalidOperationException("Platform connection is required.");
    var keyDirectory = builder.Configuration["TenantCredentials:KeyDirectory"]
        ?? throw new InvalidOperationException("Tenant credential key directory is required.");
    builder.Services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(platformConnection,
        postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "platform")));
    builder.Services.AddSingleton<ITenantCredentialKeyProvider>(new FileTenantCredentialKeyProvider(keyDirectory));
    builder.Services.AddScoped<TenantCredentialReader>();
    builder.Services.AddScoped<TenantAccessResolver>();
    builder.Services.AddScoped<TenantRequestContext>();
    builder.Services.AddScoped<ITenantRequestContext>(sp => sp.GetRequiredService<TenantRequestContext>());
    builder.Services.AddSingleton<TenantArchivalRunner>();
    if (builder.Configuration.GetValue<bool>("MultiTenancy:EnableMaintenanceJobs"))
        builder.Services.AddHostedService<TenantMaintenanceScheduler>();
}

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
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    });

builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
builder.Services.AddSignalR();

builder.Services.AddScoped<ISparePartService, SparePartService>();
builder.Services.AddScoped<IWarehouseStockService, WarehouseStockService>();
builder.Services.AddScoped<IWarehouseSupplyService, WarehouseSupplyService>();
builder.Services.AddScoped<IDispatchRequestService, DispatchRequestService>();
builder.Services.AddScoped<IWarehouseTransactionService, WarehouseTransactionService>();
builder.Services.AddScoped<IWarehouseReportService, WarehouseReportService>();
builder.Services.AddScoped<IWorkPermitService, WorkPermitService>();
builder.Services.AddScoped<IInstalledPartService, InstalledPartService>();
builder.Services.AddScoped<IMaintenanceDashboardService, MaintenanceDashboardService>();

var jwtKey = builder.Configuration["JwtSettings:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.StartsWith("{"))
{
    jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
}
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (!builder.Environment.IsProduction())
    {
        jwtKey = "FuelTrack2026!SecureJwtKey12345678901234567890";
    }
    else
    {
        throw new InvalidOperationException("JwtSettings:Key is not configured. Set it via environment variable JWT_KEY or user secrets.");
    }
}
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

builder.Services.AddHttpClient();

var healthChecks = builder.Services.AddHealthChecks()
    .AddDbContextCheck<FuelTrackDbContext>(name: "database", tags: ["ready"]);

var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConn))
{
    healthChecks.AddRedis(redisConn, name: "redis", tags: ["ready", "infrastructure"]);
}

healthChecks.AddCheck<ModbusGatewayHealthCheck>("modbus-gateway", tags: ["ready", "iot"]);

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

if (!builder.Configuration.GetValue<bool>("MultiTenancy:Enabled") &&
    !builder.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase) && !builder.Environment.IsDevelopment())
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
        {
            CommandTimeout = TimeSpan.FromMinutes(10),
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            SchemaName = "Hangfire",
            PrepareSchemaIfNecessary = true
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

var enableSwagger = builder.Configuration.GetValue<bool>("EnableSwagger");
if (app.Environment.IsDevelopment() || enableSwagger)
{
    if (!app.Environment.IsDevelopment())
    {
        app.Use(async (context, next) =>
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                return;
            }
            await next();
        });
    }

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FuelTrack API v1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "FuelTrack API v2");
    });
}

app.UseAuthentication();
if (builder.Configuration.GetValue<bool>("MultiTenancy:Enabled"))
    app.UseMiddleware<TenantResolutionMiddleware>();
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

    RecurringJob.AddOrUpdate<DatabaseMaintenanceJob>(
        "database-maintenance",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Weekly(DayOfWeek.Sunday, 2));

    RecurringJob.AddOrUpdate<DataArchivalJob>(
        "data-retention-archival",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Monthly(1, 3));
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

if (!tenantProofMode)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    // SQL Server cold-start warm-up with retry; other providers use their own initialization.
    if (dbContext.Database.IsSqlServer())
    {
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(dbContext.Database.GetConnectionString())
                {
                    InitialCatalog = "master"
                };
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString.ConnectionString);
                await connection.OpenAsync();
                Console.WriteLine($"[Startup] SQL warm-up OK on attempt {attempt}");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Startup] SQL warm-up attempt {attempt} failed: {ex.Message}");
                if (attempt == 5) throw;
                await Task.Delay(5000 * attempt);
            }

        }
        dbContext.Database.SetCommandTimeout(180);
    }

    if (dbContext.Database.IsSqlite())
    {
        startupLogger.LogInformation("Ensuring SQLite database schema is created...");
        await dbContext.Database.EnsureCreatedAsync();
        startupLogger.LogInformation("SQLite database schema verified");
    }
    else if (dbContext.Database.IsRelational())
    {
        startupLogger.LogInformation("Applying EF Core migrations...");
        await dbContext.Database.MigrateAsync();
        startupLogger.LogInformation("EF Core migrations completed successfully");
    }

    async Task RetryAsync(string name, Func<Task> operation, int maxAttempts = 5)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex)
            {
                startupLogger.LogWarning("Startup operation '{Name}' failed on attempt {Attempt}: {Message}", name, attempt, ex.Message);
                if (attempt == maxAttempts) throw;
                await Task.Delay(5000 * attempt);
            }
        }
    }

    // Seed module registry and generate permissions from it
    await RetryAsync("ModuleRegistrySeed", () => FuelTrack.Infrastructure.Data.ModuleRegistrySeed.SeedAsync(dbContext));
    var permGen = new FuelTrack.Infrastructure.Data.PermissionGenerator(dbContext);
    await RetryAsync("PermissionGenerator", () => permGen.GenerateAllPermissionsAsync());
    if (dbContext.Database.IsRelational())
    {
        await Task.Delay(1000);
    }

    // Seed roles and admin user
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleService = new FuelTrack.Infrastructure.Services.RoleService(roleManager, userManager, dbContext, scope.ServiceProvider.GetRequiredService<ICacheService>());
    await RetryAsync("RoleService.Init", () => roleService.InitAsync());

    // Validate secrets in production before proceeding
    if (app.Environment.IsProduction())
    {
        var secretsValidator = app.Services.GetRequiredService<ISecretsValidator>();
        var validationResult = secretsValidator.Validate();
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join("; ", validationResult.Errors);
            startupLogger.LogCritical("Startup secrets validation failed: {Errors}", errorMessages);
            throw new InvalidOperationException($"Startup secrets validation failed: {errorMessages}");
        }
    }

    // Ensure admin@ft.com exists with SystemOwner (and Admin) role and full scope
    const string adminEmail = "admin@ft.com";
    var adminPassword = builder.Configuration["AdminSettings:DefaultPassword"]
        ?? throw new InvalidOperationException("AdminSettings:DefaultPassword is not configured.");
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
        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (createResult.Succeeded)
        {
            adminUser.CreatedBy = adminUser.Id;
            await userManager.UpdateAsync(adminUser);
            await userManager.AddToRoleAsync(adminUser, "Admin");
            await userManager.AddToRoleAsync(adminUser, "SystemOwner");

            // Full scope = all null IDs = Admin (All)
            dbContext.UserScopes.Add(new UserScope
            {
                UserId = adminUser.Id
            });
            await dbContext.SaveChangesAsync();
        }
        else
        {
            startupLogger.LogCritical("Failed to create admin user: {Errors}", string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        // Ensure existing admin has Admin and SystemOwner roles
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            await userManager.AddToRoleAsync(adminUser, "Admin");
        if (!await userManager.IsInRoleAsync(adminUser, "SystemOwner"))
            await userManager.AddToRoleAsync(adminUser, "SystemOwner");

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

    recurringJobManager.AddOrUpdate<OilStockAlertJob>(
        "oil-stock-alerts",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily);

    recurringJobManager.AddOrUpdate<SparePartReplacementAlertJob>(
        "spare-part-replacement-alerts",
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
