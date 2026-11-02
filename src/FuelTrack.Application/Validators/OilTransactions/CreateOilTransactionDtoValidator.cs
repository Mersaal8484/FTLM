using FluentValidation;
using FuelTrack.Application.DTOs.OilTransactions;

public class CreateOilTransactionDtoValidator : AbstractValidator<CreateOilTransactionDto>
{
    public CreateOilTransactionDtoValidator()
    {
        RuleFor(x => x.SubStationId).NotEmpty();
        RuleFor(x => x.OilLookupId).NotEmpty();
        RuleFor(x => x.TransactionTypeLookupId).NotEmpty();
        RuleFor(x => x.TransactionDate).NotEmpty();
        RuleFor(x => x.QuantityLiters).NotEqual(0);
        RuleFor(x => x.Reference).MaximumLength(200).When(x => x.Reference is not null);
        RuleFor(x => x.SupplierCompany).MaximumLength(200).When(x => x.SupplierCompany is not null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}