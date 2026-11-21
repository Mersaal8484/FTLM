using FluentValidation.TestHelper;
using FuelTrack.Application.DTOs.FuelTanks;
using FuelTrack.Application.Interfaces;
using FuelTrack.Application.Validators.FuelTanks;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Validators;

public class CreateFuelTankDtoValidatorTests
{
    [Fact]
    public async Task Validate_EnterpriseMode_RejectsZeroSubStationId()
    {
        var commercialMode = new Mock<ICommercialStationModeService>();
        commercialMode.Setup(x => x.IsCommercialModeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await new CreateFuelTankDtoValidator(commercialMode.Object)
            .TestValidateAsync(CreateDto(0));

        result.ShouldHaveValidationErrorFor(x => x.SubStationId)
            .WithErrorMessage("SubStationId is required outside commercial mode.");
    }

    [Fact]
    public async Task Validate_CommercialMode_AllowsServerResolvedSubStationId()
    {
        var commercialMode = new Mock<ICommercialStationModeService>();
        commercialMode.Setup(x => x.IsCommercialModeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await new CreateFuelTankDtoValidator(commercialMode.Object)
            .TestValidateAsync(CreateDto(0));

        result.ShouldNotHaveValidationErrorFor(x => x.SubStationId);
    }

    private static CreateFuelTankDto CreateDto(int subStationId) => new(
        SubStationId: subStationId,
        FuelLookupId: 1,
        Name: "Main tank",
        Code: "KT-01",
        CapacityLiters: 5000,
        MinAlertLiters: 1000,
        DeadStockLiters: 200,
        ActualStockLiters: 4500,
        HasMeter: false,
        MeterReading: null,
        MeterReadingActual: null,
        Notes: null);
}
