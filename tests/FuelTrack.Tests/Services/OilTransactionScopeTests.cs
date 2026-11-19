using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Services;

public class OilTransactionScopeTests
{
    [Fact]
    public async Task OutsideScope_CannotReadOrChangeTransactions()
    {
        await using var db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.SubStations.Add(new SubStation { Id = 1, Name = "Site", Code = "S" });
        db.OilTransactions.Add(new OilTransaction { Id = 10, SubStationId = 1, QuantityLiters = 20 });
        await db.SaveChangesAsync();
        var scope = new Mock<IUserScopeFilterService>();
        scope.Setup(x => x.HasAccessToSubStationAsync(2, 1)).ReturnsAsync(false);
        scope.Setup(x => x.GetAccessibleSubStationIdsAsync(2)).ReturnsAsync(new List<int>());
        var service = new OilTransactionService(db, scope.Object, new StubCurrentUserService { UserId = 2, IsAdmin = false }, new CommercialStationModeService(db));

        Assert.Null(await service.GetByIdAsync(10));
        Assert.Empty(await service.GetAllAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetBySubStationAsync(1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(new CreateOilTransactionDto(SubStationId: 1), 2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateAsync(10, new UpdateOilTransactionDto(null, null, 99, null, null, null, null, null), 2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SubmitForApprovalAsync(10, 2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveAsync(10, 2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RejectAsync(10, 2));
        var transaction = await db.OilTransactions.FindAsync(10);
        Assert.Equal(20, transaction!.QuantityLiters);
        Assert.False(transaction.IsApproved);
    }

    [Fact]
    public async Task Create_RejectsTransferToInaccessibleSite()
    {
        await using var db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.SubStations.Add(new SubStation { Id = 1, Name = "Site", Code = "S" });
        await db.SaveChangesAsync();
        var scope = new Mock<IUserScopeFilterService>();
        scope.Setup(x => x.HasAccessToSubStationAsync(2, 1)).ReturnsAsync(true);
        scope.Setup(x => x.HasAccessToSubStationAsync(2, 2)).ReturnsAsync(false);
        var service = new OilTransactionService(db, scope.Object, new StubCurrentUserService { UserId = 2, IsAdmin = false }, new CommercialStationModeService(db));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(new CreateOilTransactionDto(SubStationId: 1, DestinationSubStationId: 2), 2));
        Assert.Empty(db.OilTransactions);
    }
}
