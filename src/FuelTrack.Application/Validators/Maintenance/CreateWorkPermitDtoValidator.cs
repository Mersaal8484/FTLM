using FluentValidation;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;

namespace FuelTrack.Application.Validators.Maintenance;

public class CreateWorkPermitDtoValidator : AbstractValidator<CreateWorkPermitDto>
{
    private readonly ICommercialStationModeService? _commercialMode;

    public CreateWorkPermitDtoValidator(ICommercialStationModeService? commercialMode = null)
    {
        _commercialMode = commercialMode;

        RuleFor(x => x.SubStationId)
            .MustAsync(HasTechnicalSiteOrCommercialModeAsync)
            .WithMessage("SubStationId is required outside commercial mode.");

        RuleFor(x => x.MaintenanceTypeLookupId).GreaterThan(0);
        RuleFor(x => x.DepartmentLookupId).GreaterThan(0);
        RuleFor(x => x.PriorityLookupId).GreaterThan(0);
        RuleFor(x => x.PermitDate).NotEmpty();
        RuleFor(x => x.ProblemDescription).NotEmpty().MaximumLength(1000);
    }

    private async Task<bool> HasTechnicalSiteOrCommercialModeAsync(CreateWorkPermitDto _, int subStationId, CancellationToken cancellationToken)
    {
        if (_commercialMode is not null && await _commercialMode.IsCommercialModeAsync(cancellationToken))
            return true;
        return subStationId > 0;
    }
}
