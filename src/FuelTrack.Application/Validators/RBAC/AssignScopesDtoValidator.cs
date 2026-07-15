using FluentValidation;
using FuelTrack.Application.DTOs.RBAC;

namespace FuelTrack.Application.Validators.RBAC;

public class AssignScopesDtoValidator : AbstractValidator<AssignScopesDto>
{
    public AssignScopesDtoValidator()
    {
        RuleFor(x => x.Scopes)
            .NotNull().WithMessage("Scopes list is required.");

        When(x => x.Scopes is { Count: > 0 }, () =>
        {
            RuleFor(x => x.Scopes)
                .Must(scopes => !(scopes!.Any(s => s.BranchId == null && s.StationId == null && s.SubStationId == null) && scopes.Count > 1))
                .WithMessage("Global scope cannot be combined with other scopes.");

            RuleForEach(x => x.Scopes)
                .Must(s => s.BranchId != null || (s.StationId == null && s.SubStationId == null))
                .WithMessage("Station requires a Branch.");

            RuleForEach(x => x.Scopes)
                .Must(s => s.StationId != null || s.SubStationId == null)
                .WithMessage("SubStation requires a Station.");
        });
    }
}
