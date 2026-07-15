using FluentValidation;
using FuelTrack.Application.DTOs.Readings;

namespace FuelTrack.Application.Validators.Readings;

public class ApproveReadingDtoValidator : AbstractValidator<ApproveReadingDto>
{
    public ApproveReadingDtoValidator()
    {
        RuleFor(x => x.ApprovedById).NotEmpty();
    }
}
