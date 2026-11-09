using FluentValidation;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Domain.Models;

namespace FuelTrack.Application.Validators.Operations;

public sealed class UpdateStationOperationalSettingsDtoValidator : AbstractValidator<UpdateStationOperationalSettingsDto>
{
    public UpdateStationOperationalSettingsDtoValidator()
    {
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(80);
        RuleFor(x => x.ProducedVsSentWarningThresholdPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.OilPressureUnit).Must(unit => unit is "bar" or "psi" or "kPa");
        RuleFor(x => x.ReadingPhotoMaxBytes).Equal(StationOperationalSettings.DefaultReadingPhotoMaxBytes);
        RuleFor(x => x.MissingDataIntervalMinutes).InclusiveBetween(5, 1440)
            .Must(interval => 1440 % interval == 0)
            .WithMessage("Missing-data interval must divide one day exactly.");
        RuleFor(x => x.AverageLoadWarningTolerancePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.FuelReconciliationWarningThresholdPercent).InclusiveBetween(0, 100);
    }
}
