using FluentValidation.TestHelper;
using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.Interfaces;
using FuelTrack.Application.Validators.OilTransactions;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Validators;

public class CreateOilTransactionDtoValidatorTests
{
    [Fact]
    public async Task Validate_EnterpriseMode_RejectsZeroSubStationId()
    {
        var commercialModeMock = new Mock<ICommercialStationModeService>();
        commercialModeMock.Setup(s => s.IsCommercialModeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var validator = new CreateOilTransactionDtoValidator(commercialModeMock.Object);
        var dto = new CreateOilTransactionDto(
            SubStationId: 0,
            OilLookupId: 1,
            TransactionTypeLookupId: 1,
            TransactionDate: DateTime.UtcNow,
            QuantityLiters: 100);

        var result = await validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.SubStationId)
            .WithErrorMessage("SubStationId is required outside commercial mode.");
    }

    [Fact]
    public async Task Validate_CommercialMode_AllowsZeroSubStationId()
    {
        var commercialModeMock = new Mock<ICommercialStationModeService>();
        commercialModeMock.Setup(s => s.IsCommercialModeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var validator = new CreateOilTransactionDtoValidator(commercialModeMock.Object);
        var dto = new CreateOilTransactionDto(
            SubStationId: 0,
            OilLookupId: 1,
            TransactionTypeLookupId: 1,
            TransactionDate: DateTime.UtcNow,
            QuantityLiters: 100);

        var result = await validator.TestValidateAsync(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.SubStationId);
    }
}
