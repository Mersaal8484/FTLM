using FluentValidation;
using FuelTrack.Application.DTOs.Stocktakes;

namespace FuelTrack.Application.Validators.Stocktakes;

public sealed class CreateStationStocktakeDtoValidator : AbstractValidator<CreateStationStocktakeDto>
{
    public CreateStationStocktakeDtoValidator()
    {
        RuleFor(x => x.StocktakeDate).NotEqual(default(DateOnly));
        RuleFor(x => x.ClientRequestId).NotEqual(Guid.Empty);
        RuleFor(x => x.AttachmentIds).NotEmpty();
        RuleFor(x => x).Must(x => x.FuelLines.Count + x.OilLines.Count > 0)
            .WithMessage("At least one fuel or oil line is required.");
        RuleForEach(x => x.FuelLines).ChildRules(line =>
        {
            line.RuleFor(x => x.FuelTankId).GreaterThan(0);
            line.RuleFor(x => x.CountedLiters).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.DipLevelCm).GreaterThanOrEqualTo(0).When(x => x.DipLevelCm.HasValue);
        });
        RuleForEach(x => x.OilLines).ChildRules(line =>
        {
            line.RuleFor(x => x.OilId).GreaterThan(0);
            line.RuleFor(x => x.CountedLiters).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class RejectStationStocktakeDtoValidator : AbstractValidator<RejectStationStocktakeDto>
{
    public RejectStationStocktakeDtoValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
}
