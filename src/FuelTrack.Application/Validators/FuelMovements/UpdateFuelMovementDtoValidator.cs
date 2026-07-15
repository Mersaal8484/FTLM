using FluentValidation;
using FuelTrack.Application.DTOs.FuelMovements;

namespace FuelTrack.Application.Validators.FuelMovements;

public class UpdateFuelMovementDtoValidator : AbstractValidator<UpdateFuelMovementDto>
{
    public UpdateFuelMovementDtoValidator()
    {
        When(x => x.Reference is not null, () => RuleFor(x => x.Reference).MaximumLength(200));
        When(x => x.Notes is not null, () => RuleFor(x => x.Notes).MaximumLength(1000));
    }
}
