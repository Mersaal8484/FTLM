using FluentValidation;
using FuelTrack.Application.DTOs.Generators;
using FuelTrack.Application.Interfaces;

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
        RuleFor(x => x.DailyTankId).MustAsync(RequireDailyTankInCommercialModeAsync)
            .WithMessage("A daily tank is required when creating a generator in commercial mode.");
    }

    private async Task<bool> HasTechnicalSiteOrCommercialModeAsync(CreateGeneratorDto _, int subStationId, CancellationToken cancellationToken)
    {
        if (_commercialMode is not null && await _commercialMode.IsCommercialModeAsync(cancellationToken))
            return true;
        return subStationId > 0;
    }

    private async Task<bool> RequireDailyTankInCommercialModeAsync(CreateGeneratorDto _, int? dailyTankId, CancellationToken cancellationToken)
    {
        if (_commercialMode is null)
            return true;
        bool isCommercial = await _commercialMode.IsCommercialModeAsync(cancellationToken);
        if (isCommercial && !dailyTankId.HasValue)
            return false;
        return true;
    }
}
