using FluentValidation;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Validators.Maintenance;

public class BulkBaselineRequestDtoValidator : AbstractValidator<BulkBaselineRequestDto>
{
    public BulkBaselineRequestDtoValidator()
    {
        RuleFor(x => x.GeneratorId)
            .GreaterThan(0).WithMessage("Generator ID is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one baseline item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServiceItemId)
                .GreaterThan(0).WithMessage("Service item ID is required.");

            item.RuleFor(i => i.HoursAtService)
                .GreaterThanOrEqualTo(0).WithMessage("Hours at service must be greater than or equal to zero.");

            item.RuleFor(i => i.OccurredAtUtc)
                .NotEmpty().WithMessage("Occurred date/time is required.");
        });
    }
}
