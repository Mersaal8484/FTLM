using FluentValidation;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Validators.Maintenance;

public class SaveServiceIntervalDefinitionDtoValidator : AbstractValidator<SaveServiceIntervalDefinitionDto>
{
    public SaveServiceIntervalDefinitionDtoValidator()
    {
        RuleFor(x => x.GeneratorModelKey)
            .NotEmpty().WithMessage("Generator model key is required.")
            .MaximumLength(128).WithMessage("Model key cannot exceed 128 characters.");

        RuleFor(x => x.ServiceItemId)
            .GreaterThan(0).WithMessage("Service item ID is required.");

        RuleFor(x => x.IntervalHours)
            .GreaterThan(0).WithMessage("Interval hours must be greater than zero.");

        RuleFor(x => x.WarningLeadHours)
            .GreaterThan(0).When(x => x.WarningLeadHours.HasValue)
            .WithMessage("Warning lead hours must be greater than zero.");
    }
}
