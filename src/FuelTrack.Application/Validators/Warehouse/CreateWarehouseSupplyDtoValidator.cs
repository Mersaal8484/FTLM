using FluentValidation;
using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Validators.Warehouse;

public class CreateWarehouseSupplyDtoValidator : AbstractValidator<CreateWarehouseSupplyDto>
{
    public CreateWarehouseSupplyDtoValidator()
    {
        RuleFor(x => x.StationId).GreaterThan(0);
        RuleFor(x => x.SupplyDate).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.SparePartId).GreaterThan(0);
            items.RuleFor(i => i.Quantity).GreaterThan(0);
            items.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
