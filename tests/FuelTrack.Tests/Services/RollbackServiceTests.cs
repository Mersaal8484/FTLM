using FuelTrack.Application.DTOs.Rollback;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public class RollbackServiceTests : IDisposable
{
    private readonly RollbackService _service;
    private readonly FuelTrackDbContext _db;
    private readonly int _userId;
    private readonly int _subStationId;

    public RollbackServiceTests()
    {
        var o = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"RollbackTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(o);

        _service = new RollbackService(_db);

        _userId = 1;
        _subStationId = 1;

        _db.Users.Add(new User
        {
            Id = _userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            FullName = "Test User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SubStations.Add(new SubStation { Id = _subStationId, Name = "Test SubStation", Code = "TSS" });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsAllActiveAuditLogs()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userId,
            Action = "Create",
            EntityName = "SubStation",
            EntityId = _subStationId,
            Timestamp = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetHistoryAsync();

        var item = Assert.Single(result);
        Assert.Equal("Create", item.Action);
        Assert.Equal("Test User", item.UserName);
        Assert.Equal("SubStation", item.EntityName);
    }

    [Fact]
    public async Task GetHistoryAsync_ExcludesInactiveEntries()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userId,
            Action = "Delete",
            EntityName = "SubStation",
            EntityId = _subStationId,
            Timestamp = DateTime.UtcNow,
            IsActive = false
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetHistoryAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEmptyWhenNoLogs()
    {
        var result = await _service.GetHistoryAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckRollbackAsync_ReturnsCannotRollbackWhenNotFound()
    {
        var result = await _service.CheckRollbackAsync(999);

        Assert.False(result.CanRollback);
        Assert.Equal("Audit log not found", result.Reason);
    }

    [Fact]
    public async Task CheckRollbackAsync_ReturnsCanRollbackForCreateAction()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userId,
            Action = "Create",
            EntityName = "SubStation",
            EntityId = _subStationId,
            Timestamp = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var auditId = await _db.AuditLogs.Select(x => x.Id).FirstAsync();

        var result = await _service.CheckRollbackAsync(auditId);

        Assert.True(result.CanRollback);
        Assert.Contains("Delete", result.Summary);
    }

    [Fact]
    public async Task CheckRollbackAsync_ReturnsCannotRollbackWhenEntityMissing()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userId,
            Action = "Update",
            EntityName = "SubStation",
            EntityId = 999,
            OldValues = "{}",
            Timestamp = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var auditId = await _db.AuditLogs.Select(x => x.Id).FirstAsync();

        var result = await _service.CheckRollbackAsync(auditId);

        Assert.False(result.CanRollback);
        Assert.Equal("Entity no longer exists in database", result.Reason);
    }

    [Fact]
    public async Task CheckRollbackAsync_ReturnsCanRollbackForUpdateAction()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userId,
            Action = "Update",
            EntityName = "SubStation",
            EntityId = _subStationId,
            OldValues = "{}",
            Timestamp = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var auditId = await _db.AuditLogs.Select(x => x.Id).FirstAsync();

        var result = await _service.CheckRollbackAsync(auditId);

        Assert.True(result.CanRollback);
        Assert.Contains("Restore", result.Summary);
    }

    [Fact]
    public async Task ExecuteRollbackAsync_ThrowsWhenNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.ExecuteRollbackAsync(999, _userId));
    }

    [Fact]
    public async Task ExecuteRollbackAsync_SoftDeletesEntityForCreateAction()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userId,
            Action = "Create",
            EntityName = "SubStation",
            EntityId = _subStationId,
            Timestamp = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var auditId = await _db.AuditLogs.Select(x => x.Id).FirstAsync();

        await _service.ExecuteRollbackAsync(auditId, _userId);

        var subStation = await _db.SubStations.FindAsync(_subStationId);
        Assert.NotNull(subStation);
        Assert.False(subStation.IsActive);
    }

    [Fact]
    public async Task ExecuteRollbackAsync_RestoresOldValuesForUpdateAction()
    {
        var subStation = await _db.SubStations.FindAsync(_subStationId);
        Assert.NotNull(subStation);
        subStation.Name = "Modified Name";
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userId,
            Action = "Update",
            EntityName = "SubStation",
            EntityId = _subStationId,
            OldValues = """{"Name":"Test SubStation"}""",
            NewValues = """{"Name":"Modified Name"}""",
            Timestamp = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var auditId = await _db.AuditLogs.Select(x => x.Id).FirstAsync();

        await _service.ExecuteRollbackAsync(auditId, _userId);

        var restored = await _db.SubStations.FindAsync(_subStationId);
        Assert.NotNull(restored);
        Assert.Equal("Test SubStation", restored.Name);
    }

    [Fact]
    public async Task ExecuteRollbackAsync_CreatesRollbackAuditLog()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userId,
            Action = "Create",
            EntityName = "SubStation",
            EntityId = _subStationId,
            Timestamp = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var auditId = await _db.AuditLogs.Select(x => x.Id).FirstAsync();

        await _service.ExecuteRollbackAsync(auditId, _userId);

        var rollbackLog = await _db.AuditLogs
            .Where(x => x.Action == "Rollback")
            .FirstOrDefaultAsync();
        Assert.NotNull(rollbackLog);
        Assert.Equal(_userId, rollbackLog.UserId);
        Assert.Equal("SubStation", rollbackLog.EntityName);
        Assert.Equal(_subStationId, rollbackLog.EntityId);
    }

    [Fact]
    public async Task ExecuteRollbackAsync_ThrowsForDeleteAction()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userId,
            Action = "Delete",
            EntityName = "SubStation",
            EntityId = _subStationId,
            Timestamp = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var auditId = await _db.AuditLogs.Select(x => x.Id).FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteRollbackAsync(auditId, _userId));
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
