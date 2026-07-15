using FluentValidation;
using FuelTrack.Application.DTOs.SubStations;

namespace FuelTrack.Application.Validators.SubStations;

public class CreateSubStationDtoValidator : AbstractValidator<CreateSubStationDto>
{
    public CreateSubStationDtoValidator()
    {
        RuleFor(x => x.StationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Location).MaximumLength(500).When(x => x.Location is not null);
        RuleFor(x => x.CapacityKW).GreaterThan(0).When(x => x.CapacityKW.HasValue);
    }
}
