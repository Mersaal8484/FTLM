using FuelTrack.Application.DTOs.Audits;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly AuditService _service;
    private readonly FuelTrackDbContext _db;

    public AuditServiceTests()
    {
        var o = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"AuditTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(o);

        _service = new AuditService(_db, new StubCurrentUserService());
    }

    [Fact]
    public async Task GetPagedAsync_NoFilter_ReturnsAll()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new AuditLog { UserId = 1, Action = "Update", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-2) },
            new AuditLog { UserId = 2, Action = "Delete", EntityName = "Generator", EntityId = 5, Timestamp = DateTime.UtcNow.AddDays(-3) }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new AuditFilterParams());

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_WithUserId_FiltersCorrectly()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new AuditLog { UserId = 2, Action = "Update", EntityName = "Generator", EntityId = 5, Timestamp = DateTime.UtcNow.AddDays(-2) }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new AuditFilterParams(UserId: 1));

        Assert.Single(result.Items);
        Assert.Equal("Create", result.Items[0].Action);
    }

    [Fact]
    public async Task GetPagedAsync_WithAction_FiltersCorrectly()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new AuditLog { UserId = 1, Action = "Update", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-2) },
            new AuditLog { UserId = 2, Action = "Delete", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-3) }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new AuditFilterParams(Action: "Create"));

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetPagedAsync_WithEntityName_FiltersCorrectly()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new AuditLog { UserId = 1, Action = "Update", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-2) },
            new AuditLog { UserId = 2, Action = "Create", EntityName = "Generator", EntityId = 5, Timestamp = DateTime.UtcNow.AddDays(-3) }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new AuditFilterParams(EntityName: "Generator"));

        Assert.Single(result.Items);
        Assert.Equal("Generator", result.Items[0].EntityName);
    }

    [Fact]
    public async Task GetPagedAsync_WithStartDate_FiltersCorrectly()
    {
        var now = DateTime.UtcNow;
        _db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Station", EntityId = 10, Timestamp = now.AddDays(-5) },
            new AuditLog { UserId = 1, Action = "Update", EntityName = "Station", EntityId = 10, Timestamp = now.AddDays(-1) }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new AuditFilterParams(StartDate: now.AddDays(-3)));

        Assert.Single(result.Items);
        Assert.Equal("Update", result.Items[0].Action);
    }

    [Fact]
    public async Task GetPagedAsync_WithEndDate_FiltersCorrectly()
    {
        var now = DateTime.UtcNow;
        _db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Station", EntityId = 10, Timestamp = now.AddDays(-5) },
            new AuditLog { UserId = 1, Action = "Update", EntityName = "Station", EntityId = 10, Timestamp = now.AddDays(-1) }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new AuditFilterParams(EndDate: now.AddDays(-3)));

        Assert.Single(result.Items);
        Assert.Equal("Create", result.Items[0].Action);
    }

    [Fact]
    public async Task GetPagedAsync_Pagination_WorksCorrectly()
    {
        for (int i = 0; i < 25; i++)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = 1,
                Action = "Create",
                EntityName = "Station",
                EntityId = i,
                Timestamp = DateTime.UtcNow.AddDays(-i)
            });
        }
        await _db.SaveChangesAsync();

        var page1 = await _service.GetPagedAsync(new AuditFilterParams(Page: 1, PageSize: 10));
        var page2 = await _service.GetPagedAsync(new AuditFilterParams(Page: 2, PageSize: 10));

        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(10, page2.Items.Count);
        Assert.NotEqual(page1.Items[0].Id, page2.Items[0].Id);
    }

    [Fact]
    public async Task GetPagedAsync_FilterByActionContains_MatchesPartial()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new AuditLog { UserId = 1, Action = "Delete", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-2) }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new AuditFilterParams(Action: "elet"));

        Assert.Single(result.Items);
        Assert.Equal("Delete", result.Items[0].Action);
    }

    [Fact]
    public async Task GetPagedAsync_InactiveLogs_Excluded()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-1), IsActive = false },
            new AuditLog { UserId = 1, Action = "Update", EntityName = "Station", EntityId = 10, Timestamp = DateTime.UtcNow.AddDays(-2) }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new AuditFilterParams());

        Assert.Single(result.Items);
        Assert.Equal("Update", result.Items[0].Action);
    }

    [Fact]
    public async Task ExportCsvAsync_ReturnsValidCsv()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Station", EntityId = 10, OldValues = null, NewValues = "{\"Name\":\"Main\"}", IpAddress = "10.0.0.1", Timestamp = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc) },
            new AuditLog { UserId = 2, Action = "Update", EntityName = "Generator", EntityId = 5, OldValues = "{\"Name\":\"Old\"}", NewValues = "{\"Name\":\"New\"}", IpAddress = "10.0.0.2", Timestamp = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc) }
        );
        await _db.SaveChangesAsync();

        var bytes = await _service.ExportCsvAsync(new AuditFilterParams());
        var csv = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Contains("Usuário;Ação;Entidade;Id Entidade;Ip;Timestamp", csv);
        Assert.Contains("Create;Station;10;10.0.0.1;2026-06-01 10:00:00", csv);
        Assert.Contains("Update;Generator;5;10.0.0.2;2026-06-02 12:00:00", csv);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
