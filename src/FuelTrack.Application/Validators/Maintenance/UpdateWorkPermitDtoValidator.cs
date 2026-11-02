using FluentValidation;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Validators.Maintenance;

public class UpdateWorkPermitDtoValidator : AbstractValidator<UpdateWorkPermitDto>
{
    public UpdateWorkPermitDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        When(x => x.ProblemDescription is not null, () => RuleFor(x => x.ProblemDescription).MaximumLength(1000));
    }
}
