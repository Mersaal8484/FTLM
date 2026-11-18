using FluentValidation;
using FuelTrack.Application.DTOs.FuelTanks;
using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.Validators.FuelTanks;

public class CreateFuelTankDtoValidator : AbstractValidator<CreateFuelTankDto>
{
    public CreateFuelTankDtoValidator()
    {
        RuleFor(x => x.SubStationId).NotEmpty();
        RuleFor(x => x.FuelLookupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CapacityLiters).GreaterThan(0);
        RuleFor(x => x.MinAlertLiters).GreaterThanOrEqualTo(0).When(x => x.MinAlertLiters.HasValue);
        RuleFor(x => x.DeadStockLiters).GreaterThanOrEqualTo(0).When(x => x.DeadStockLiters.HasValue);
        RuleFor(x => x.ActualStockLiters).GreaterThanOrEqualTo(0).When(x => x.ActualStockLiters.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
        RuleFor(x => x.Shape)
            .Must(s => !s.HasValue || Enum.IsDefined(typeof(TankShape), s.Value))
            .WithMessage("Unsupported tank shape. Only vertical cylinder with conical bottom (Shape = 0) is supported.");
    }
}
