using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.Operations;

public sealed record SubmitOperationalCaptureDto(
    Guid ClientRequestId,
    int SubStationId,
    IReadOnlyList<GeneratorCaptureLineDto> GeneratorReadings,
    IReadOnlyList<TankLevelCaptureLineDto> TankLevelReadings,
    IReadOnlyList<FeederLoadCaptureLineDto> FeederLoadReadings,
    IReadOnlyList<MeterCaptureLineDto> MeterReadings,
    DateTime? SubmissionCapturedAtUtc = null,
    DateTime? ClientSentAtUtc = null,
    bool IsOfflineQueue = false);

public sealed record GeneratorCaptureLineDto(
    int GeneratorId, DateTime CapturedAtUtc, IReadOnlyList<int> PhotoAttachmentIds, bool IsRunning,
    decimal? LineVoltageVolts, decimal? CurrentAmps, decimal? WaterTemperatureCelsius,
    decimal? OilPressure, decimal? OilTemperatureCelsius, decimal? ActiveLoadKw,
    decimal? ApparentLoadKva, decimal? ReactiveLoadKvar, decimal? FrequencyHz,
    decimal? OilAddedLitres, decimal? PowerFactor,
    Guid? ClientLineId = null, decimal? PeakLoadKw = null, decimal? AirPressureBar = null,
    DateTime? CapturedAtOverrideUtc = null);

public sealed record TankLevelCaptureLineDto(
    int FuelTankId, DateTime CapturedAtUtc, IReadOnlyList<int> PhotoAttachmentIds, decimal LiquidLevelCm,
    Guid? ClientLineId = null, TankEventType EventType = TankEventType.Level,
    Guid? LinkedClientLineId = null, decimal? BeforeLiquidLevelCm = null,
    IReadOnlyList<int>? BeforePhotoAttachmentIds = null, decimal? DeliveryNoteLitres = null,
    DateTime? CapturedAtOverrideUtc = null);

public sealed record FeederLoadCaptureLineDto(
    int FeederId, DateTime CapturedAtUtc, IReadOnlyList<int> PhotoAttachmentIds,
    decimal? ActiveLoadKw, decimal? ApparentLoadKva, Guid? ClientLineId = null,
    DateTime? CapturedAtOverrideUtc = null);

public sealed record MeterCaptureLineDto(
    MeterReadingSubjectType SubjectType, int SubjectId, int? MeterId, MeterReadingKind Kind,
    DateTime CapturedAtUtc, IReadOnlyList<int> PhotoAttachmentIds, decimal RawValue,
    decimal? MultiplierSnapshot, bool IsMeterReset, string? ResetNote,
    Guid? ClientLineId = null, DateOnly? BusinessDate = null, DateTime? CapturedAtOverrideUtc = null);

public sealed record OperationalSubmissionDto(
    int Id, int SubStationId, DateTime ReceivedAtUtc, string Status,
    int GeneratorReadingCount, int TankLevelReadingCount, int FeederLoadReadingCount, int MeterReadingCount,
    IReadOnlyList<OperationalLineResultDto>? Lines = null);

/// <summary>Idempotent per-device line acknowledgement for future offline sync.</summary>
public sealed record OperationalLineResultDto(Guid ClientLineId, string Outcome, string? Reason = null, int? EntityId = null);
public sealed record VoidOperationalReadingDto(string Reason);

public sealed record OperationalHistoryItemDto(
    int Id, int SubStationId, string SubStationName, DateTime ReceivedAtUtc, string Status,
    int GeneratorReadingCount, int TankLevelReadingCount, int FeederLoadReadingCount, int MeterReadingCount);

public sealed record OperationalCaptureReferenceDto(
    int SubStationId, string Name, string Code, string TimeZone,
    IReadOnlyList<OperationalGeneratorReferenceDto> Generators,
    IReadOnlyList<OperationalTankReferenceDto> Tanks,
    IReadOnlyList<OperationalFeederReferenceDto> Feeders,
    IReadOnlyList<OperationalMeterReferenceDto> Meters);

public sealed record OperationalGeneratorReferenceDto(int Id, string Name, string? Model, int? MeterId);
public sealed record OperationalTankReferenceDto(int Id, string Name, string Code, bool IsCalibrated);
public sealed record OperationalFeederReferenceDto(int Id, string Name, string Code, decimal MeterMultiplier, int? MeterId);
public sealed record OperationalMeterReferenceDto(int Id, string Name, string? SerialNumber, int? GeneratorId);

/// <summary>Latest raw and official counter values for a capture site's generators and feeders.</summary>
public sealed record PreviousMeterReadingsDto(IReadOnlyList<PreviousMeterReadingDto> Readings);

public sealed record PreviousMeterReadingDto(
    MeterReadingSubjectType SubjectType,
    int SubjectId,
    LatestMeterReadingDto? LatestAny,
    LatestMeterReadingDto? LatestOfficialDailyClosing,
    decimal? RecentAverageDelta);

public sealed record LatestMeterReadingDto(
    decimal RawValue,
    decimal MultiplierSnapshot,
    DateTime CapturedAtUtc,
    MeterReadingKind Kind,
    OperationalReadingStatus Status,
    bool IsMeterReset);
