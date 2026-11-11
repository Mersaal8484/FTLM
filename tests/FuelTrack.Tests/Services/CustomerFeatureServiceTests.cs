using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class CustomerFeatureServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly CustomerFeatureService _service;

    public CustomerFeatureServiceTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"CustomerFeatureTests_{Guid.NewGuid()}").Options);
        _service = new CustomerFeatureService(_db);
    }

    [Fact]
    public async Task IsOperationalDataNewModeAsync_WhenMissing_ReturnsFalse()
    {
        var isNewMode = await _service.IsOperationalDataNewModeAsync();
        Assert.False(isNewMode);
    }

    [Fact]
    public async Task IsOperationalDataNewModeAsync_WhenDisabled_ReturnsFalse()
    {
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = false,
            Value = "new"
        });
        await _db.SaveChangesAsync();

        var isNewMode = await _service.IsOperationalDataNewModeAsync();
        Assert.False(isNewMode);
    }

    [Fact]
    public async Task IsOperationalDataNewModeAsync_WhenEnabledAndLegacy_ReturnsFalse()
    {
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = true,
            Value = "legacy"
        });
        await _db.SaveChangesAsync();

        var isNewMode = await _service.IsOperationalDataNewModeAsync();
        Assert.False(isNewMode);
    }

    [Fact]
    public async Task IsOperationalDataNewModeAsync_WhenEnabledAndNew_ReturnsTrue()
    {
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = true,
            Value = "new"
        });
        await _db.SaveChangesAsync();

        var isNewMode = await _service.IsOperationalDataNewModeAsync();
        Assert.True(isNewMode);
    }

    [Fact]
    public async Task IsOperationalDataNewModeAsync_WhenEnabledAndNullValue_ReturnsFalse()
    {
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = true,
            Value = null
        });
        await _db.SaveChangesAsync();

        var isNewMode = await _service.IsOperationalDataNewModeAsync();
        Assert.False(isNewMode);
    }

    public void Dispose() => _db.Dispose();
}
