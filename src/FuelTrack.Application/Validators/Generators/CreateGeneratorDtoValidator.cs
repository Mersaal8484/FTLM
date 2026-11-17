using FluentValidation;
using FuelTrack.Application.DTOs.Generators;

namespace FuelTrack.Application.Validators.Generators;

public class CreateGeneratorDtoValidator : AbstractValidator<CreateGeneratorDto>
{
    public CreateGeneratorDtoValidator()
    {
        RuleFor(x => x.SubStationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(50);
        When(x => x.NominalPower.HasValue, () => RuleFor(x => x.NominalPower!.Value).GreaterThanOrEqualTo(0));
        When(x => x.FuelConsumptionRatePerHour.HasValue, () => RuleFor(x => x.FuelConsumptionRatePerHour!.Value).GreaterThanOrEqualTo(0));
    }
}
