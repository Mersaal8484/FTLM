using FluentValidation;
using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Validators.Warehouse;

public class UpdateSparePartDtoValidator : AbstractValidator<UpdateSparePartDto>
{
    public UpdateSparePartDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        When(x => x.Name is not null, () => RuleFor(x => x.Name).MaximumLength(200));
        When(x => x.PartNumber is not null, () => RuleFor(x => x.PartNumber).MaximumLength(100));
        When(x => x.PartCategoryLookupId.HasValue, () => RuleFor(x => x.PartCategoryLookupId!.Value).GreaterThan(0));
        When(x => x.UnitLookupId.HasValue, () => RuleFor(x => x.UnitLookupId!.Value).GreaterThan(0));
        When(x => x.UnitPrice.HasValue, () => RuleFor(x => x.UnitPrice!.Value).GreaterThanOrEqualTo(0));
        When(x => x.MinStockQuantity.HasValue, () => RuleFor(x => x.MinStockQuantity!.Value).GreaterThanOrEqualTo(0));
    }
}
