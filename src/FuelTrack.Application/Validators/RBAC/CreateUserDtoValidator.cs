using FluentValidation;
using FuelTrack.Application.DTOs.RBAC;

namespace FuelTrack.Application.Validators.RBAC;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username must not exceed 100 characters.")
            .EmailAddress().WithMessage("Username must be a valid email address.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");

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
