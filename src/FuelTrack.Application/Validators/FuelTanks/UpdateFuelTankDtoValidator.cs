using FluentValidation;
using FuelTrack.Application.DTOs.FuelTanks;
using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.Validators.FuelTanks;

public class UpdateFuelTankDtoValidator : AbstractValidator<UpdateFuelTankDto>
{
    public UpdateFuelTankDtoValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name).MaximumLength(200));
        When(x => x.Code is not null, () => RuleFor(x => x.Code).MaximumLength(50));
        When(x => x.CapacityLiters.HasValue, () => RuleFor(x => x.CapacityLiters!.Value).GreaterThan(0));
        When(x => x.MinAlertLiters.HasValue, () => RuleFor(x => x.MinAlertLiters!.Value).GreaterThanOrEqualTo(0));
        When(x => x.DeadStockLiters.HasValue, () => RuleFor(x => x.DeadStockLiters!.Value).GreaterThanOrEqualTo(0));
        When(x => x.ActualStockLiters.HasValue, () => RuleFor(x => x.ActualStockLiters!.Value).GreaterThanOrEqualTo(0));
        When(x => x.Notes is not null, () => RuleFor(x => x.Notes).MaximumLength(1000));
#pragma warning disable CS0618 // Type or member is obsolete
        When(x => x.Shape.HasValue, () =>
        {
            RuleFor(x => x.Shape)
                .Must(s => s != TankShape.HorizontalCylinderWithConicalEnds)
                .WithMessage("Horizontal cylinder with conical ends (Shape = 1) is not supported. Tanks must use vertical cylinder with conical bottom (Shape = 0).");
        });
#pragma warning restore CS0618
    }
}
