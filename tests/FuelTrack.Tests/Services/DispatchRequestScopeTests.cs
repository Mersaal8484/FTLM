using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Services;

public class DispatchRequestScopeTests
{
    [Fact]
    public async Task OutsideScope_CannotCreateChangeApproveOrIssueRequest()
    {
        await using var db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var status = new Lookup { Id = 1, Code = "Pending", Name = "Pending" };
        db.SubStations.Add(new SubStation { Id = 1, StationId = 1, Name = "Site", Code = "S" });
        db.DispatchRequests.Add(new DispatchRequest { Id = 10, SubStationId = 1, StationId = 1, StatusLookupId = 1, StatusLookup = status, RequestNumber = "D-1" });
        await db.SaveChangesAsync();
        var scope = new Mock<IUserScopeFilterService>();
        scope.Setup(x => x.HasAccessToSubStationAsync(2, 1)).ReturnsAsync(false);
        scope.Setup(x => x.GetAccessibleSubStationIdsAsync(2)).ReturnsAsync(new List<int>());
        var service = new DispatchRequestService(db, new StubCurrentUserService { UserId = 2, IsAdmin = false }, scope.Object);
        Assert.Null(await service.GetByIdAsync(10));
        Assert.Empty(await service.GetApprovalDataAsync(10));
        var create = await service.CreateAsync(new CreateDispatchRequestDto(1, 1, null, null, null, []), 2);
        var update = await service.UpdateAsync(10, new UpdateDispatchRequestDto(10, null, "changed", null), 2);
        var approve = await service.ApproveAsync(10, new ApproveDispatchRequestDto([], null), 2);
        var reject = await service.RejectAsync(10, "reason", 2);
        var issue = await service.IssueAsync(10, 2);
        var deletion = await service.DeleteAsync(10);
        Assert.All(new[] { create.StatusCode, update.StatusCode, approve.StatusCode, reject.StatusCode, issue.StatusCode, deletion.StatusCode }, code => Assert.Equal(403, code));
        var dispatch = await db.DispatchRequests.FindAsync(10);
        Assert.Null(dispatch!.DeletedAt);
        Assert.NotEqual("changed", dispatch.Purpose);
    }

    [Fact]
    public async Task Create_RejectsMismatchedStationAndWorkPermit()
    {
        await using var db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.SubStations.Add(new SubStation { Id = 1, StationId = 1, Name = "Site", Code = "S" });
        db.WorkPermits.Add(new WorkPermit { Id = 5, SubStationId = 2, PermitNumber = "OTHER" });
        await db.SaveChangesAsync();
        var service = new DispatchRequestService(db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService());
        var wrongStation = await service.CreateAsync(new CreateDispatchRequestDto(1, 2, null, null, null, []), 1);
        var wrongPermit = await service.CreateAsync(new CreateDispatchRequestDto(1, 1, 5, null, null, []), 1);
        Assert.False(wrongStation.IsSuccess);
        Assert.False(wrongPermit.IsSuccess);
        Assert.Empty(db.DispatchRequests);
    }
}
