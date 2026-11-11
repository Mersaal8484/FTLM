using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FuelTrack.Infrastructure
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FuelTrackDbContext>
    {
        public FuelTrackDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../FuelTrack.API"))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("Platform");
            if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("Provide via"))
                throw new InvalidOperationException("A DefaultConnection or Platform connection is required for EF design-time operations.");

            var optionsBuilder = new DbContextOptionsBuilder<FuelTrackDbContext>();
            if (configuration["Database:Provider"]?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
                optionsBuilder.UseSqlServer(connectionString);
            else
                optionsBuilder.UseNpgsql(connectionString);
            optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            return new FuelTrackDbContext(optionsBuilder.Options);
        }
    }
}
