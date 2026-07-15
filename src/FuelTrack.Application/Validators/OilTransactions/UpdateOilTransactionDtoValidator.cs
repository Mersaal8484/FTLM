using FluentValidation;
using FuelTrack.Application.DTOs.OilTransactions;

public class UpdateOilTransactionDtoValidator : AbstractValidator<UpdateOilTransactionDto>
{
    public UpdateOilTransactionDtoValidator()
    {
        RuleFor(x => x.InvoiceNumber).MaximumLength(200).When(x => x.InvoiceNumber is not null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
