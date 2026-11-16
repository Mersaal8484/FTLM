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
        RuleFor(x => x.NominalPower).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FuelConsumptionRatePerHour).GreaterThanOrEqualTo(0);
    }
}
