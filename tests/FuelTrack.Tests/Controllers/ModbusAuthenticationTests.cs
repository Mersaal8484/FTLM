using FuelTrack.API.Controllers;
using FuelTrack.Application.DTOs.Modbus;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FuelTrack.Tests.Controllers;

public class ModbusAuthenticationTests
{
    private const string Key = "test-gateway-key-at-least-32-characters";
    private static readonly ModbusReadingDto Reading = new(1, 42, DateTime.UtcNow);

    [Theory]
    [InlineData(null, null)]
    [InlineData("short", "short")]
    [InlineData(Key, null)]
    [InlineData(Key, "wrong-key")]
    public async Task MissingWeakOrWrongKey_CannotSave(string? configured, string? supplied)
    {
        var (controller, service) = CreateController(configured, supplied);
        Assert.IsType<UnauthorizedResult>(await controller.ReceiveModbusReading(Reading));
        service.Verify(x => x.SaveModbusReadingAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task ValidKey_SavesReading()
    {
        var (controller, service) = CreateController(Key, Key);
        Assert.IsType<OkObjectResult>(await controller.ReceiveModbusReading(Reading));
        service.Verify(x => x.SaveModbusReadingAsync(Reading.GeneratorId, Reading.FuelLevelLiters, Reading.ReadingTimestamp), Times.Once);
    }

    [Fact]
    public async Task ValidKey_InvalidReading_IsRejected()
    {
        var (controller, service) = CreateController(Key, Key);
        Assert.IsType<BadRequestResult>(await controller.ReceiveModbusReading(new ModbusReadingDto(1, -1, DateTime.UtcNow)));
        service.Verify(x => x.SaveModbusReadingAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task ValidKey_EmitsReadingInvalidationAfterSaving()
    {
        var notifier = new Mock<IRealtimeNotifier>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ModbusGateway:ApiKey"] = Key }).Build();
        var controller = new ReadingsController(Mock.Of<IReadingService>(), notifier.Object, Mock.Of<IWorkflowNotificationService>(), config, NullLogger<ReadingsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers["X-Modbus-Key"] = Key;
        await controller.ReceiveModbusReading(Reading);
        notifier.Verify(x => x.NotifyAllAsync("ReadingCreated", It.IsAny<object>()), Times.Once);
    }

    private static (ReadingsController, Mock<IReadingService>) CreateController(string? configured, string? supplied)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ModbusGateway:ApiKey"] = configured
        }).Build();
        var service = new Mock<IReadingService>();
        var controller = new ReadingsController(service.Object, Mock.Of<IRealtimeNotifier>(), Mock.Of<IWorkflowNotificationService>(), config, NullLogger<ReadingsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        if (supplied != null) controller.Request.Headers["X-Modbus-Key"] = supplied;
        return (controller, service);
    }
}
