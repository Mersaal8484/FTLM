using FluentValidation;
using FuelTrack.Application.DTOs.Generators;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.Validators.Generators;

public class CreateGeneratorDtoValidator : AbstractValidator<CreateGeneratorDto>
{
    private readonly ICommercialStationModeService? _commercialMode;

    public CreateGeneratorDtoValidator(ICommercialStationModeService? commercialMode = null)
    {
        _commercialMode = commercialMode;
        RuleFor(x => x.SubStationId).MustAsync(HasTechnicalSiteOrCommercialModeAsync)
            .WithMessage("SubStationId is required outside commercial mode.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(50);
        When(x => x.NominalPower.HasValue, () => RuleFor(x => x.NominalPower!.Value)
            .GreaterThanOrEqualTo(0)
            .OverridePropertyName(nameof(CreateGeneratorDto.NominalPower)));
        When(x => x.FuelConsumptionRatePerHour.HasValue, () => RuleFor(x => x.FuelConsumptionRatePerHour!.Value)
            .GreaterThanOrEqualTo(0)
            .OverridePropertyName(nameof(CreateGeneratorDto.FuelConsumptionRatePerHour)));
        When(x => x.DailyTank is not null, () =>
        {
            RuleFor(x => x.DailyTank!.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DailyTank!.Code).NotEmpty().MaximumLength(50);
            RuleFor(x => x.DailyTank!.FuelLookupId).GreaterThan(0);
            RuleFor(x => x.DailyTank!.CapacityLiters).GreaterThan(0).When(x => !x.DailyTank!.CapacityHeightCm.HasValue);
            RuleFor(x => x.DailyTank!.CapacityHeightCm!.Value).GreaterThan(0).When(x => x.DailyTank!.CapacityHeightCm.HasValue);
            RuleFor(x => x.DailyTank!.LitresPerCm).NotNull().GreaterThan(0)
                .When(x => x.DailyTank!.CalibrationMode == TankCalibrationMode.LitresPerCm);
        });
    }

    private async Task<bool> HasTechnicalSiteOrCommercialModeAsync(CreateGeneratorDto _, int subStationId, CancellationToken cancellationToken)
    {
        if (_commercialMode is not null && await _commercialMode.IsCommercialModeAsync(cancellationToken))
            return true;
        return subStationId > 0;
    }
}
