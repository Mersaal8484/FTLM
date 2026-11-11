using FluentValidation;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Validators.Maintenance;

public class CreateServiceItemDtoValidator : AbstractValidator<CreateServiceItemDto>
{
    public CreateServiceItemDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Service item code is required.")
            .MaximumLength(64).WithMessage("Code cannot exceed 64 characters.");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required.")
            .MaximumLength(128).WithMessage("Arabic name cannot exceed 128 characters.");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required.")
            .MaximumLength(128).WithMessage("English name cannot exceed 128 characters.");

        RuleFor(x => x.SuggestedIntervalHours)
            .GreaterThan(0).When(x => x.SuggestedIntervalHours.HasValue)
            .WithMessage("Suggested interval hours must be positive.");
    }
}
