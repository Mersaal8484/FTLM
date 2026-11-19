using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Services;

public class WorkPermitScopeTests
{
    [Fact]
    public async Task MutationsOutsideScopeAreDeniedBeforeChanges()
    {
        await using var db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var status = new Lookup { Id = 88, Code = "Draft", Name = "Draft" };
        db.SubStations.Add(new SubStation { Id = 1, Name = "Site", Code = "S" });
        db.WorkPermits.Add(new WorkPermit { Id = 1, SubStationId = 1, StatusLookupId = 88, StatusLookup = status, PermitNumber = "WP-1" });
        await db.SaveChangesAsync();
        var scope = new Mock<IUserScopeFilterService>();
        scope.Setup(s => s.HasAccessToSubStationAsync(2, 1)).ReturnsAsync(false);
        var service = new WorkPermitService(db, new StubCurrentUserService { UserId = 2, IsAdmin = false }, scope.Object, new CommercialStationModeService(db));

        var create = await service.CreateAsync(new CreateWorkPermitDto(1, null, 1, 1, 1, DateTime.UtcNow, "x", null, null, null, null, null, []), 2);
        var update = await service.UpdateAsync(1, new UpdateWorkPermitDto(1, null, null, null, null, null, "changed", null, null, null, null, null, null, null, null), 2);
        var statusChange = await service.UpdateStatusAsync(1, new UpdateWorkPermitStatusDto(88, null, null, null, null), 2);
        var deletion = await service.DeleteAsync(1);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CompleteTaskAsync(1, 1, 2));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddLogAsync(1, "change", null, 2));

        Assert.Equal(403, create.StatusCode);
        Assert.Equal(403, update.StatusCode);
        Assert.Equal(403, statusChange.StatusCode);
        Assert.Equal(403, deletion.StatusCode);
        Assert.Null((await db.WorkPermits.FindAsync(1))!.DeletedAt);
        Assert.NotEqual("changed", (await db.WorkPermits.FindAsync(1))!.ProblemDescription);
    }
}
