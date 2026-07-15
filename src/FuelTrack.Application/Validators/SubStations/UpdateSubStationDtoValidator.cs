using FluentValidation;
using FuelTrack.Application.DTOs.SubStations;

namespace FuelTrack.Application.Validators.SubStations;

public class UpdateSubStationDtoValidator : AbstractValidator<UpdateSubStationDto>
{
    public UpdateSubStationDtoValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name).MaximumLength(200));
        When(x => x.Code is not null, () => RuleFor(x => x.Code).MaximumLength(50));
        When(x => x.Location is not null, () => RuleFor(x => x.Location).MaximumLength(500));
        When(x => x.CapacityKW.HasValue, () => RuleFor(x => x.CapacityKW!.Value).GreaterThan(0));
    }
}
