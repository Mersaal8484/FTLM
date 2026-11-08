using FluentValidation;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Validators.Operations;

public sealed class UpdateCustomerFeatureSettingDtoValidator : AbstractValidator<UpdateCustomerFeatureSettingDto>
{
    public UpdateCustomerFeatureSettingDtoValidator()
    {
        RuleFor(x => x.Value).MaximumLength(500);
    }
}
