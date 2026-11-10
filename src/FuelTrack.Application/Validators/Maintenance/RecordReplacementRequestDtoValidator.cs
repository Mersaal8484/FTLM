using FluentValidation;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Validators.Maintenance;

public class RecordReplacementRequestDtoValidator : AbstractValidator<RecordReplacementRequestDto>
{
    public RecordReplacementRequestDtoValidator()
    {
        RuleFor(x => x.GeneratorId)
            .GreaterThan(0).WithMessage("Generator ID is required.");

        RuleFor(x => x.ServiceItemId)
            .GreaterThan(0).WithMessage("Service item ID is required.");

        RuleFor(x => x.HoursAtService)
            .GreaterThanOrEqualTo(0).WithMessage("Hours at service must be greater than or equal to zero.");

        RuleFor(x => x.OccurredAtUtc)
            .NotEmpty().WithMessage("Occurred date/time is required.");
    }
}
