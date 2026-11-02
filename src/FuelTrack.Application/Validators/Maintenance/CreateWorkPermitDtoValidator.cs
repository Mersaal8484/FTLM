using FluentValidation;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Validators.Maintenance;

public class CreateWorkPermitDtoValidator : AbstractValidator<CreateWorkPermitDto>
{
    public CreateWorkPermitDtoValidator()
    {
        RuleFor(x => x.SubStationId).GreaterThan(0);
        RuleFor(x => x.MaintenanceTypeLookupId).GreaterThan(0);
        RuleFor(x => x.DepartmentLookupId).GreaterThan(0);
        RuleFor(x => x.PriorityLookupId).GreaterThan(0);
        RuleFor(x => x.PermitDate).NotEmpty();
        RuleFor(x => x.ProblemDescription).NotEmpty().MaximumLength(1000);
    }
}
