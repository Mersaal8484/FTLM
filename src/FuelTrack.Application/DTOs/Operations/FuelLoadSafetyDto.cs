using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.Operations;

public sealed record FuelConsumptionReportDto(
    int GeneratorId, DateTime FromUtc, DateTime ToUtc, decimal ConsumptionLitres,
    decimal RefillLitres, decimal? ProducedKwh, decimal? KwhPerL,
    IReadOnlyList<string> Warnings);

public sealed record FuelReconciliationLineDto(
    int TankId, string TankName, decimal TransferOutLitres, decimal TransferInLitres,
    decimal ReceiptLitres, decimal MeasuredIncreaseLitres, decimal DifferenceLitres,
    IReadOnlyList<string> Warnings);

public sealed record FuelReconciliationReportDto(
    int SubStationId, DateTime FromUtc, DateTime ToUtc,
    IReadOnlyList<FuelReconciliationLineDto> Lines);

public sealed record OverloadEventDto(
    int Id, int GeneratorId, string GeneratorName, int TriggerReadingId,
    DateTime StartedAtUtc, DateTime? ResolvedAtUtc, decimal PeakPercent);

public sealed record GeneratorOperatingLimitDto(
    int Id, int GeneratorId, GeneratorOperatingParameter Parameter,
    decimal? Minimum, decimal? Maximum);

public sealed record SaveGeneratorOperatingLimitDto(
    GeneratorOperatingParameter Parameter, decimal? Minimum, decimal? Maximum);

public sealed record OperationalTrendPointDto(
    DateTime CapturedAtUtc, decimal? ActiveLoadKw, decimal? ApparentLoadKva,
    decimal? PowerFactor, decimal? FrequencyHz, bool IsEstimated, IReadOnlyList<string> Markers);

public sealed record OperationalDailyLogDto(
    int GeneratorId, DateOnly BusinessDate, IReadOnlyList<OperationalTrendPointDto> Rows);
