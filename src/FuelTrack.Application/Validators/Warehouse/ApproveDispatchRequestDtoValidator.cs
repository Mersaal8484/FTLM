using FluentValidation;
using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Validators.Warehouse;

public class ApproveDispatchRequestDtoValidator : AbstractValidator<ApproveDispatchRequestDto>
{
    public ApproveDispatchRequestDtoValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.DispatchRequestItemId).GreaterThan(0);
            items.RuleFor(i => i.ApprovedQuantity).GreaterThanOrEqualTo(0);
        });
    }
}
