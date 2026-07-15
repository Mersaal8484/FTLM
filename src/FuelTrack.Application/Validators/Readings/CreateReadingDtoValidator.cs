using FluentValidation;
using FuelTrack.Application.DTOs.Readings;

namespace FuelTrack.Application.Validators.Readings;

public class CreateReadingDtoValidator : AbstractValidator<CreateReadingDto>
{
    public CreateReadingDtoValidator()
    {
        RuleFor(x => x.GeneratorId).NotEmpty();

        RuleFor(x => x.ReadingDate).NotEmpty();
        RuleFor(x => x.FuelLookupId).NotEmpty();
        RuleFor(x => x.PrevRunningHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrentRunningHours).GreaterThan(0);
        RuleFor(x => x.PrevEnergyKWh).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrentEnergyKWh).GreaterThan(0);
        RuleFor(x => x.StandardFuelRate).GreaterThan(0);
        RuleFor(x => x.OilUsedLiters).GreaterThan(0).When(x => x.OilUsedLiters.HasValue);
    }
}
