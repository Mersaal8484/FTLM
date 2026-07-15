using FluentValidation;
using FuelTrack.Application.DTOs.Readings;

namespace FuelTrack.Application.Validators.Readings;

public class UpdateReadingDtoValidator : AbstractValidator<UpdateReadingDto>
{
    public UpdateReadingDtoValidator()
    {
        RuleFor(x => x).Must(x =>
            x.PrevRunningHours.HasValue || x.CurrentRunningHours.HasValue ||
            x.PrevEnergyKWh.HasValue || x.CurrentEnergyKWh.HasValue ||
            x.StandardFuelRate.HasValue || x.OilLookupId.HasValue ||
            x.OilUsedLiters.HasValue)
            .WithMessage("At least one field must be provided.");

        RuleFor(x => x.StandardFuelRate).GreaterThan(0).When(x => x.StandardFuelRate.HasValue);
        RuleFor(x => x.OilUsedLiters).GreaterThan(0).When(x => x.OilUsedLiters.HasValue);
    }
}
