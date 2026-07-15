using FluentValidation;
using FuelTrack.Application.DTOs.Readings;

namespace FuelTrack.Application.Validators.Readings;

public class RejectReadingDtoValidator : AbstractValidator<RejectReadingDto>
{
    public RejectReadingDtoValidator()
    {
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RejectedById).NotEmpty();
    }
}
