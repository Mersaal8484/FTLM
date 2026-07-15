using FluentValidation;
using FuelTrack.Application.DTOs.Lookups;

namespace FuelTrack.Application.Validators.Lookups;

public class UpdateLookupDtoValidator : AbstractValidator<UpdateLookupDto>
{
    public UpdateLookupDtoValidator()
    {
        When(x => x.Category is not null, () => RuleFor(x => x.Category).MaximumLength(100));
        When(x => x.Code is not null, () => RuleFor(x => x.Code).MaximumLength(100));
        When(x => x.Name is not null, () => RuleFor(x => x.Name).MaximumLength(500));
    }
}
