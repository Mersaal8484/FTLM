using FuelTrack.API.Jobs;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FuelTrack.Tests.Jobs;

public class CleanupExpiredRefreshTokensJobTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly CleanupExpiredRefreshTokensJob _job;

    public CleanupExpiredRefreshTokensJobTests()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"CleanupRefreshTokensTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(options);

        _job = new CleanupExpiredRefreshTokensJob(_db, NullLogger<CleanupExpiredRefreshTokensJob>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_RemovesExpiredTokens()
    {
        var userId = 1;
        _db.Users.Add(new User { Id = userId, UserName = "test", Email = "test@test.com", IsActive = true });
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = "expired1",
            ExpiresAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        });
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = "valid1",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });
        await _db.SaveChangesAsync();

        await _job.ExecuteAsync();

        Assert.Equal(1, await _db.RefreshTokens.CountAsync());
        Assert.DoesNotContain(await _db.RefreshTokens.Select(t => t.Token).ToListAsync(), t => t == "expired1");
    }

    [Fact]
    public async Task ExecuteAsync_RemovesRevokedTokensOlderThan30Days()
    {
        var userId = 1;
        _db.Users.Add(new User { Id = userId, UserName = "test", Email = "test@test.com", IsActive = true });
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = "revoked1",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddDays(-40)
        });
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = "valid_revoked",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddDays(-1)
        });
        await _db.SaveChangesAsync();

        await _job.ExecuteAsync();

        Assert.Equal(1, await _db.RefreshTokens.CountAsync());
        Assert.DoesNotContain(await _db.RefreshTokens.Select(t => t.Token).ToListAsync(), t => t == "revoked1");
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
