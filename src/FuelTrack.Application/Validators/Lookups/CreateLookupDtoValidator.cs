using FluentValidation;
using FuelTrack.Application.DTOs.Lookups;

namespace FuelTrack.Application.Validators.Lookups;

public class CreateLookupDtoValidator : AbstractValidator<CreateLookupDto>
{
    public CreateLookupDtoValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
    }
}
