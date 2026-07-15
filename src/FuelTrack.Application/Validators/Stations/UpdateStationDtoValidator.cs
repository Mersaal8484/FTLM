using FluentValidation;
using FuelTrack.Application.DTOs.Stations;

namespace FuelTrack.Application.Validators.Stations;

public class UpdateStationDtoValidator : AbstractValidator<UpdateStationDto>
{
    public UpdateStationDtoValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name).MaximumLength(200));
        When(x => x.Code is not null, () => RuleFor(x => x.Code).MaximumLength(50));
        When(x => x.Notes is not null, () => RuleFor(x => x.Notes).MaximumLength(1000));
    }
}
