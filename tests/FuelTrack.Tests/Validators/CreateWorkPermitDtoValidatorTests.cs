using FluentValidation.TestHelper;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Application.Validators.Maintenance;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Validators;

public class CreateWorkPermitDtoValidatorTests
{
    [Fact]
    public async Task Validate_EnterpriseMode_RejectsZeroSubStationId()
    {
        var commercialModeMock = new Mock<ICommercialStationModeService>();
        commercialModeMock.Setup(s => s.IsCommercialModeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var validator = new CreateWorkPermitDtoValidator(commercialModeMock.Object);
        var dto = new CreateWorkPermitDto(
            SubStationId: 0,
            MaintenanceTypeLookupId: 1,
            DepartmentLookupId: 1,
            PriorityLookupId: 1,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Test problem");

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

        var validator = new CreateWorkPermitDtoValidator(commercialModeMock.Object);
        var dto = new CreateWorkPermitDto(
            SubStationId: 0,
            MaintenanceTypeLookupId: 1,
            DepartmentLookupId: 1,
            PriorityLookupId: 1,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Test problem");

        var result = await validator.TestValidateAsync(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.SubStationId);
    }

    [Fact]
    public async Task Validate_NullCommercialModeService_RejectsZeroSubStationId()
    {
        var validator = new CreateWorkPermitDtoValidator(null);
        var dto = new CreateWorkPermitDto(
            SubStationId: 0,
            MaintenanceTypeLookupId: 1,
            DepartmentLookupId: 1,
            PriorityLookupId: 1,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Test problem");

        var result = await validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.SubStationId);
    }
}
