using FluentValidation;
using FuelTrack.Application.DTOs.FuelMovements;

namespace FuelTrack.Application.Validators.FuelMovements;

public class CreateFuelMovementDtoValidator : AbstractValidator<CreateFuelMovementDto>
{
    public CreateFuelMovementDtoValidator()
    {
        RuleFor(x => x.SubStationId).GreaterThan(0);
        RuleFor(x => x.FuelLookupId).GreaterThan(0);
        RuleFor(x => x.QuantityLiters).GreaterThan(0);
        RuleFor(x => x.TransactionDate).NotEmpty();
        RuleFor(x => x.Reference).MaximumLength(200).When(x => x.Reference is not null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
        RuleFor(x => x.CorrectionReason).MaximumLength(500).When(x => x.CorrectionReason is not null);
        RuleFor(x => x.SupplierCompany).MaximumLength(200).When(x => x.SupplierCompany is not null);
        RuleFor(x => x.DriverName).MaximumLength(200).When(x => x.DriverName is not null);
        RuleFor(x => x.DriverIdCard).MaximumLength(50).When(x => x.DriverIdCard is not null);
        RuleFor(x => x.TruckNumber).MaximumLength(50).When(x => x.TruckNumber is not null);
        RuleFor(x => x.InvoiceNumber).MaximumLength(100).When(x => x.InvoiceNumber is not null);
        RuleFor(x => x.WarehouseReceiptNumber).MaximumLength(100).When(x => x.WarehouseReceiptNumber is not null);
    }
}
