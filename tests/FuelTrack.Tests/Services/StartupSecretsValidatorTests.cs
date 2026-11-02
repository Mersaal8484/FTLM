using System.Collections.Generic;
using FuelTrack.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FuelTrack.Tests.Services;

public class StartupSecretsValidatorTests
{
    [Fact]
    public void Validate_ValidProductionConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["JwtSettings:Key"] = "FuelTrackProductionMasterKey992817264827101!#$",
                ["JwtSettings:Issuer"] = "FuelTrack.API",
                ["JwtSettings:Audience"] = "FuelTrack.Clients",
                ["DataEncryption:Key"] = "DnrpIAixmM+tEYmWJaNkU6iWnxFVcSAJ6NWTjQxuB+w=",
                ["ConnectionStrings:DefaultConnection"] = "Server=sqlserver;Database=FuelTrackDb;User Id=sa;Password=ProdPassword123!;",
                ["AdminSettings:DefaultPassword"] = "SuperAdmin2026!Secure",
                ["Cors:AllowedOrigins:0"] = "https://fueltrack.company.org"
            })
            .Build();

        var validator = new StartupSecretsValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MissingOrWeakDataEncryptionKey_InProduction_ReturnsErrors()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["JwtSettings:Key"] = "FuelTrackProductionMasterKey992817264827101!#$",
                ["JwtSettings:Issuer"] = "FuelTrack.API",
                ["JwtSettings:Audience"] = "FuelTrack.Clients",
                ["DataEncryption:Key"] = "{DATA_ENCRYPTION_KEY}", // placeholder!
                ["ConnectionStrings:DefaultConnection"] = "Server=sqlserver;Database=FuelTrackDb;User Id=sa;Password=ProdPassword123!;",
                ["AdminSettings:DefaultPassword"] = "SuperAdmin2026!Secure",
                ["Cors:AllowedOrigins:0"] = "https://fueltrack.company.org"
            })
            .Build();

        var validator = new StartupSecretsValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("DataEncryption:Key"));
    }

    [Fact]
    public void Validate_WildcardCors_InProduction_ReturnsError()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["JwtSettings:Key"] = "FuelTrackProductionMasterKey992817264827101!#$",
                ["JwtSettings:Issuer"] = "FuelTrack.API",
                ["JwtSettings:Audience"] = "FuelTrack.Clients",
                ["DataEncryption:Key"] = "DnrpIAixmM+tEYmWJaNkU6iWnxFVcSAJ6NWTjQxuB+w=",
                ["ConnectionStrings:DefaultConnection"] = "Server=sqlserver;Database=FuelTrackDb;User Id=sa;Password=ProdPassword123!;",
                ["AdminSettings:DefaultPassword"] = "SuperAdmin2026!Secure",
                ["Cors:AllowedOrigins:0"] = "*" // Wildcard error
            })
            .Build();

        var validator = new StartupSecretsValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("CORS must not use wildcard"));
    }
}
