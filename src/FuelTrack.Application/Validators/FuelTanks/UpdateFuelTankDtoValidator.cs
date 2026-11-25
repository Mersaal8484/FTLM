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
        When(x => x.CapacityLiters.HasValue && !x.CapacityHeightCm.HasValue, () => RuleFor(x => x.CapacityLiters!.Value).GreaterThan(0));
        When(x => x.CapacityHeightCm.HasValue, () => RuleFor(x => x.CapacityHeightCm!.Value).GreaterThan(0));
        When(x => x.CalibrationMode == TankCalibrationMode.LitresPerCm, () => RuleFor(x => x.LitresPerCm).NotNull().GreaterThan(0));
        When(x => x.MinAlertLiters.HasValue, () => RuleFor(x => x.MinAlertLiters!.Value).GreaterThanOrEqualTo(0));
        When(x => x.DeadStockLiters.HasValue, () => RuleFor(x => x.DeadStockLiters!.Value).GreaterThanOrEqualTo(0));
        When(x => x.ActualStockLiters.HasValue, () => RuleFor(x => x.ActualStockLiters!.Value).GreaterThanOrEqualTo(0));
        When(x => x.Notes is not null, () => RuleFor(x => x.Notes).MaximumLength(1000));
        When(x => x.Shape.HasValue, () =>
        {
            RuleFor(x => x.Shape)
                .Must(s => !s.HasValue || Enum.IsDefined(typeof(TankShape), s.Value))
                .WithMessage("Unsupported tank shape. Only vertical cylinder with conical bottom (Shape = 0) is supported.");
        });
        When(x => x.MinAlertHeightCm.HasValue, () => RuleFor(x => x.MinAlertHeightCm!.Value).GreaterThanOrEqualTo(0));
        When(x => x.DeadStockHeightCm.HasValue, () => RuleFor(x => x.DeadStockHeightCm!.Value).GreaterThanOrEqualTo(0));
        When(x => x.ActualStockHeightCm.HasValue, () => RuleFor(x => x.ActualStockHeightCm!.Value).GreaterThanOrEqualTo(0));
    }
}
