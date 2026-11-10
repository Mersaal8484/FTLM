using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FuelTrack.Tests.Services;

public sealed class AlertGeneratorServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly Mock<INotificationService> _notifications = new();
    private readonly AlertGeneratorService _service;

    public AlertGeneratorServiceTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"AlertGeneratorTests_{Guid.NewGuid()}").Options);
        _service = new AlertGeneratorService(_db, _notifications.Object);
    }

    [Fact]
    public async Task CheckFuelStockLevelsAsync_LowTank_PublishesScopedFuelAlert()
    {
        var subStation = new SubStation { Id = 1, Name = "SS", Code = "SS", StationId = 1 };
        _db.SubStations.Add(subStation);
        _db.FuelTanks.Add(new FuelTank { Id = 10, Name = "Tank", SubStationId = 1, SubStation = subStation, ActualStockLiters = 25, MinAlertLiters = 50, CapacityLiters = 100 });
        await _db.SaveChangesAsync();
        _notifications.Setup(x => x.PublishScopedAsync(It.IsAny<CreateNotificationDto>(), null, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationDto(1, 1, "", "", "fuel_low", "danger", false, "FuelTank", 10, null, null, DateTime.UtcNow));

        var created = await _service.CheckFuelStockLevelsAsync();

        Assert.Equal(1, created);
        _notifications.Verify(x => x.PublishScopedAsync(It.Is<CreateNotificationDto>(n => n.NotificationType == "fuel_low" && n.RelatedEntityId == 10), null,
            It.Is<IReadOnlyCollection<string>>(m => m.Contains("fuel_tanks")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckFuelStockLevelsAsync_NoRecipients_DoesNotReportCreatedAlert()
    {
        var subStation = new SubStation { Id = 1, Name = "SS", Code = "SS", StationId = 1 };
        _db.SubStations.Add(subStation);
        _db.FuelTanks.Add(new FuelTank { Id = 10, Name = "Tank", SubStationId = 1, SubStation = subStation, ActualStockLiters = 25, MinAlertLiters = 50, CapacityLiters = 100 });
        await _db.SaveChangesAsync();
        _notifications.Setup(x => x.PublishScopedAsync(It.IsAny<CreateNotificationDto>(), null, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationDto?)null);

        var created = await _service.CheckFuelStockLevelsAsync();

        Assert.Equal(0, created);
    }

    [Fact]
    public async Task CheckFuelStockLevelsAsync_UsesUsableStockAfterDeadStock()
    {
        var subStation = new SubStation { Id = 1, Name = "SS", Code = "SS", StationId = 1 };
        _db.SubStations.Add(subStation);
        _db.FuelTanks.Add(new FuelTank
        {
            Id = 11, Name = "Tank", SubStationId = 1, SubStation = subStation,
            ActualStockLiters = 120, DeadStockLiters = 40, MinAlertLiters = 100, CapacityLiters = 500
        });
        await _db.SaveChangesAsync();
        _notifications.Setup(x => x.PublishScopedAsync(It.IsAny<CreateNotificationDto>(), null, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationDto(2, 1, "", "", "fuel_low", "danger", false, "FuelTank", 11, null, null, DateTime.UtcNow));

        var created = await _service.CheckFuelStockLevelsAsync();

        Assert.Equal(1, created);
        _notifications.Verify(x => x.PublishScopedAsync(It.Is<CreateNotificationDto>(n => n.RelatedEntityId == 11 && n.Message.Contains("80")), null,
            It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose() => _db.Dispose();
}
