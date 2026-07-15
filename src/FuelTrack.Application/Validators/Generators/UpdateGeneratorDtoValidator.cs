using FluentValidation;
using FuelTrack.Application.DTOs.Generators;

namespace FuelTrack.Application.Validators.Generators;

public class UpdateGeneratorDtoValidator : AbstractValidator<UpdateGeneratorDto>
{
    public UpdateGeneratorDtoValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name).MaximumLength(200));
        When(x => x.SerialNumber is not null, () => RuleFor(x => x.SerialNumber).MaximumLength(50));
        When(x => x.NominalPower.HasValue, () => RuleFor(x => x.NominalPower!.Value).GreaterThan(0));
        When(x => x.ActualPowerKW.HasValue, () => RuleFor(x => x.ActualPowerKW!.Value).GreaterThan(0));
        When(x => x.FuelConsumptionRatePerHour.HasValue, () => RuleFor(x => x.FuelConsumptionRatePerHour!.Value).GreaterThan(0));
    }
}
