using FluentValidation;
using FuelTrack.Application.DTOs.RBAC;

namespace FuelTrack.Application.Validators.RBAC;

public class UpdateRoleDtoValidator : AbstractValidator<UpdateRoleDto>
{
    public UpdateRoleDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Key)
            .MaximumLength(50).WithMessage("Role key must not exceed 50 characters.")
            .Matches("^[a-z_]+$").WithMessage("Role key must contain only lowercase letters and underscores.")
            .When(x => !string.IsNullOrEmpty(x.Key));

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("Description must not exceed 200 characters.");
    }
}
