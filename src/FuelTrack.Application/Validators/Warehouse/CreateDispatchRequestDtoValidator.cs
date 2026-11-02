using FluentValidation;
using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Validators.Warehouse;

public class CreateDispatchRequestDtoValidator : AbstractValidator<CreateDispatchRequestDto>
{
    public CreateDispatchRequestDtoValidator()
    {
        RuleFor(x => x.SubStationId).GreaterThan(0);
        RuleFor(x => x.StationId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.SparePartId).GreaterThan(0);
            items.RuleFor(i => i.RequestedQuantity).GreaterThan(0);
        });
    }
}
