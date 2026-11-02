using FluentValidation;
using FuelTrack.Application.DTOs.OilTransactions;

public class UpdateOilTransactionDtoValidator : AbstractValidator<UpdateOilTransactionDto>
{
    public UpdateOilTransactionDtoValidator()
    {
        RuleFor(x => x.Reference).MaximumLength(200).When(x => x.Reference is not null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}