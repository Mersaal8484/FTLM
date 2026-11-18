using System.Security.Claims;
using FuelTrack.API.Controllers;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FuelTrack.Tests.Controllers;

public class CommercialWarehouseTests
{
    private static ClaimsPrincipal User() => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "12"), new Claim(ClaimTypes.Role, "Manager") }, "test"));
    private static ControllerContext Context() => new() { HttpContext = new DefaultHttpContext { User = User() } };

    [Fact]
    public async Task Supply_RejectsUserOutsideStationScope()
    {
        var service = new Mock<IWarehouseSupplyService>();
        var mode = new Mock<ICommercialStationModeService>();
        mode.Setup(x => x.GetScopeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CommercialStationScope(1, 2, 3));
        var filter = new Mock<IUserScopeFilterService>();
        var controller = new WarehouseSupplyController(service.Object, Mock.Of<IWorkflowNotificationService>()) { ControllerContext = Context() };
        var request = new WarehouseSupplyController.CommercialSupplyRequest(DateTime.UtcNow, "test", null, null, null, null, new() { new CreateSupplyItemDto(1, 1, 1, null, null) });
        Assert.IsType<ForbidResult>(await controller.CreateCommercial(request, mode.Object, filter.Object, CancellationToken.None));
        service.Verify(x => x.CreateAsync(It.IsAny<CreateWarehouseSupplyDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Supply_UsesServerResolvedStation()
    {
        var service = new Mock<IWarehouseSupplyService>();
        service.Setup(x => x.CreateAsync(It.IsAny<CreateWarehouseSupplyDto>(), 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseSupplyDetailDto>.Failure("test"));
        var mode = new Mock<ICommercialStationModeService>();
        mode.Setup(x => x.GetScopeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CommercialStationScope(1, 2, 3));
        var filter = new Mock<IUserScopeFilterService>();
        filter.Setup(x => x.HasFullAccessToStationAsync(12, 2)).ReturnsAsync(true);
        var controller = new WarehouseSupplyController(service.Object, Mock.Of<IWorkflowNotificationService>()) { ControllerContext = Context() };
        var request = new WarehouseSupplyController.CommercialSupplyRequest(DateTime.UtcNow, "test", null, null, null, null, new() { new CreateSupplyItemDto(1, 1, 1, null, null) });
        await controller.CreateCommercial(request, mode.Object, filter.Object, CancellationToken.None);
        service.Verify(x => x.CreateAsync(It.Is<CreateWarehouseSupplyDto>(dto => dto.StationId == 2), 12, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_RejectsUserOutsideSiteScope()
    {
        var service = new Mock<IDispatchRequestService>();
        var mode = new Mock<ICommercialStationModeService>();
        mode.Setup(x => x.GetScopeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CommercialStationScope(1, 2, 3));
        var filter = new Mock<IUserScopeFilterService>();
        var controller = new DispatchRequestController(service.Object) { ControllerContext = Context() };
        var request = new DispatchRequestController.CommercialDispatchRequest(null, "test", null, new() { new CreateDispatchItemDto(1, 1, null) });
        Assert.IsType<ForbidResult>(await controller.CreateCommercial(request, mode.Object, filter.Object, Mock.Of<IWorkPermitService>(), CancellationToken.None));
        service.Verify(x => x.CreateAsync(It.IsAny<CreateDispatchRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
