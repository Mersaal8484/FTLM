using FluentValidation;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Validators.Maintenance;

public class CreateInstalledPartDtoValidator : AbstractValidator<CreateInstalledPartDto>
{
    public CreateInstalledPartDtoValidator()
    {
        RuleFor(x => x.GeneratorId).GreaterThan(0);
        RuleFor(x => x.SparePartId).GreaterThan(0);
        RuleFor(x => x.InstallationDate).NotEmpty();
        RuleFor(x => x.InstallationHours).GreaterThanOrEqualTo(0);
    }
}
