using FluentValidation;
using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Validators.Warehouse;

public class CreateSparePartDtoValidator : AbstractValidator<CreateSparePartDto>
{
    public CreateSparePartDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PartNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PartCategoryLookupId).GreaterThan(0);
        RuleFor(x => x.UnitLookupId).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStockQuantity).GreaterThanOrEqualTo(0);
    }
}
