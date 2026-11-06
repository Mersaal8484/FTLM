using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.Operations;

public sealed record SubmitOperationalCaptureDto(
    Guid ClientRequestId,
    int SubStationId,
    IReadOnlyList<GeneratorCaptureLineDto> GeneratorReadings,
    IReadOnlyList<TankLevelCaptureLineDto> TankLevelReadings,
    IReadOnlyList<FeederLoadCaptureLineDto> FeederLoadReadings,
    IReadOnlyList<MeterCaptureLineDto> MeterReadings);

public sealed record GeneratorCaptureLineDto(
    int GeneratorId, DateTime CapturedAtUtc, IReadOnlyList<int> PhotoAttachmentIds, bool IsRunning,
    decimal? LineVoltageVolts, decimal? CurrentAmps, decimal? WaterTemperatureCelsius,
    decimal? OilPressure, decimal? OilTemperatureCelsius, decimal? ActiveLoadKw,
    decimal? ApparentLoadKva, decimal? ReactiveLoadKvar, decimal? FrequencyHz,
    decimal? OilAddedLitres, decimal? PowerFactor);

public sealed record TankLevelCaptureLineDto(
    int FuelTankId, DateTime CapturedAtUtc, IReadOnlyList<int> PhotoAttachmentIds, decimal LiquidLevelCm);

public sealed record FeederLoadCaptureLineDto(
    int FeederId, DateTime CapturedAtUtc, IReadOnlyList<int> PhotoAttachmentIds,
    decimal? ActiveLoadKw, decimal? ApparentLoadKva);

public sealed record MeterCaptureLineDto(
    MeterReadingSubjectType SubjectType, int SubjectId, int? MeterId, MeterReadingKind Kind,
    DateTime CapturedAtUtc, IReadOnlyList<int> PhotoAttachmentIds, decimal RawValue,
    decimal? MultiplierSnapshot, bool IsMeterReset, string? ResetNote);

public sealed record OperationalSubmissionDto(
    int Id, int SubStationId, DateTime ReceivedAtUtc, string Status,
    int GeneratorReadingCount, int TankLevelReadingCount, int FeederLoadReadingCount, int MeterReadingCount);

public sealed record OperationalHistoryItemDto(
    int Id, int SubStationId, string SubStationName, DateTime ReceivedAtUtc, string Status,
    int GeneratorReadingCount, int TankLevelReadingCount, int FeederLoadReadingCount, int MeterReadingCount);

public sealed record OperationalCaptureReferenceDto(
    int SubStationId, string Name, string Code,
    IReadOnlyList<OperationalGeneratorReferenceDto> Generators,
    IReadOnlyList<OperationalTankReferenceDto> Tanks,
    IReadOnlyList<OperationalFeederReferenceDto> Feeders,
    IReadOnlyList<OperationalMeterReferenceDto> Meters);

public sealed record OperationalGeneratorReferenceDto(int Id, string Name, string? Model, int? MeterId);
public sealed record OperationalTankReferenceDto(int Id, string Name, string Code, bool IsCalibrated);
public sealed record OperationalFeederReferenceDto(int Id, string Name, string Code, decimal MeterMultiplier, int? MeterId);
public sealed record OperationalMeterReferenceDto(int Id, string Name, string? SerialNumber, int? GeneratorId);
