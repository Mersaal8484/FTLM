using FluentValidation;
using FuelTrack.Application.DTOs.FuelTanks;

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
    }
}
