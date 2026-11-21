using FluentValidation;
using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.Interfaces;

namespace FuelTrack.Application.Validators.OilTransactions;

public class CreateOilTransactionDtoValidator : AbstractValidator<CreateOilTransactionDto>
{
    private readonly ICommercialStationModeService? _commercialMode;

    public CreateOilTransactionDtoValidator(ICommercialStationModeService? commercialMode = null)
    {
        _commercialMode = commercialMode;

        RuleFor(x => x.SubStationId)
            .MustAsync(HasValidSiteOrCommercialModeAsync)
            .WithMessage("SubStationId is required outside commercial mode.");

        RuleFor(x => x.OilLookupId).GreaterThan(0);
        RuleFor(x => x.QuantityLiters).NotEqual(0);
        RuleFor(x => x.AttachmentIds)
            .Must(ids => ids is null || ids.Count <= 3)
            .WithMessage("At most three attachments are allowed.")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Attachment identifiers must be unique.");
        RuleFor(x => x.Reference).MaximumLength(200).When(x => x.Reference is not null);
        RuleFor(x => x.SupplierCompany).MaximumLength(200).When(x => x.SupplierCompany is not null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }

    private async Task<bool> HasValidSiteOrCommercialModeAsync(CreateOilTransactionDto _, int subStationId, CancellationToken ct)
    {
        if (_commercialMode != null && await _commercialMode.IsCommercialModeAsync(ct))
            return true;
        return subStationId > 0;
    }
}
