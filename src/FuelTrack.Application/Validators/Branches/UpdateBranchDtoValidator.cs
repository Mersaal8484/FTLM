using FluentValidation;
using FuelTrack.Application.DTOs.Branches;

namespace FuelTrack.Application.Validators.Branches;

public class UpdateBranchDtoValidator : AbstractValidator<UpdateBranchDto>
{
    public UpdateBranchDtoValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name).MaximumLength(200));
        When(x => x.Code is not null, () => RuleFor(x => x.Code).MaximumLength(50));
        When(x => x.Notes is not null, () => RuleFor(x => x.Notes).MaximumLength(1000));
    }
}
