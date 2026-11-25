using FluentValidation;
using FuelTrack.Application.DTOs.FuelTanks;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.Validators.FuelTanks;

public class CreateFuelTankDtoValidator : AbstractValidator<CreateFuelTankDto>
{
    private readonly ICommercialStationModeService? _commercialMode;

    public CreateFuelTankDtoValidator(ICommercialStationModeService? commercialMode = null)
    {
        _commercialMode = commercialMode;

        RuleFor(x => x.SubStationId)
            .MustAsync(HasTechnicalSiteOrCommercialModeAsync)
            .WithMessage("SubStationId is required outside commercial mode.");
        RuleFor(x => x.FuelLookupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CapacityLiters).GreaterThan(0).When(x => !x.CapacityHeightCm.HasValue);
        RuleFor(x => x.CapacityHeightCm!.Value).GreaterThan(0).When(x => x.CapacityHeightCm.HasValue);
        RuleFor(x => x.LitresPerCm).NotNull().GreaterThan(0).When(x => x.CalibrationMode == TankCalibrationMode.LitresPerCm);
        RuleFor(x => x.MinAlertLiters).GreaterThanOrEqualTo(0).When(x => x.MinAlertLiters.HasValue);
        RuleFor(x => x.DeadStockLiters).GreaterThanOrEqualTo(0).When(x => x.DeadStockLiters.HasValue);
        RuleFor(x => x.ActualStockLiters).GreaterThanOrEqualTo(0).When(x => x.ActualStockLiters.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
        RuleFor(x => x.Shape)
            .Must(s => !s.HasValue || Enum.IsDefined(typeof(TankShape), s.Value))
            .WithMessage("Unsupported tank shape. Only vertical cylinder with conical bottom (Shape = 0) is supported.");
        RuleFor(x => x.MinAlertHeightCm!.Value).GreaterThanOrEqualTo(0).When(x => x.MinAlertHeightCm.HasValue);
        RuleFor(x => x.DeadStockHeightCm!.Value).GreaterThanOrEqualTo(0).When(x => x.DeadStockHeightCm.HasValue);
        RuleFor(x => x.ActualStockHeightCm!.Value).GreaterThanOrEqualTo(0).When(x => x.ActualStockHeightCm.HasValue);
    }

    private async Task<bool> HasTechnicalSiteOrCommercialModeAsync(
        CreateFuelTankDto _, int subStationId, CancellationToken cancellationToken)
    {
        if (_commercialMode is not null && await _commercialMode.IsCommercialModeAsync(cancellationToken))
            return true;

        return subStationId > 0;
    }
}
