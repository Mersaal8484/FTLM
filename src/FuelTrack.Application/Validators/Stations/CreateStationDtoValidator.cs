using FluentValidation;
using FuelTrack.Application.DTOs.Stations;

namespace FuelTrack.Application.Validators.Stations;

public class CreateStationDtoValidator : AbstractValidator<CreateStationDto>
{
    public CreateStationDtoValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
