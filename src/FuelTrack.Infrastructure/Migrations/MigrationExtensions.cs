using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Infrastructure.Migrations;

public static class MigrationExtensions
{
    public static async Task MigrateWithLockAsync(this IApplicationBuilder app, int lockTimeoutMs = 60000, CancellationToken cancellationToken = default)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
        await runner.RunAsync(lockTimeoutMs, cancellationToken);
    }
}
