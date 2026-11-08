using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.Operations;

// ── Review queue ──────────────────────────────────────────────────────────────

public sealed record PendingSubmissionDto(
    int Id,
    int SubStationId,
    string SiteName,
    DateTime ReceivedAtUtc,
    int SubmittedByUserId,
    string SubmitterName,
    int GeneratorLineCount,
    int TankLineCount,
    int FeederLineCount,
    int MeterLineCount,
    bool HasSkewWarning);

public sealed record SubmissionDetailDto(
    int Id,
    int SubStationId,
    string SiteName,
    DateTime ReceivedAtUtc,
    int SubmittedByUserId,
    string SubmitterName,
    string Status,
    IReadOnlyList<ReviewLineDto> GeneratorLines,
    IReadOnlyList<ReviewLineDto> TankLines,
    IReadOnlyList<ReviewLineDto> FeederLines,
    IReadOnlyList<ReviewMeterLineDto> MeterLines);

public sealed record ReviewLineDto(
    int Id,
    string LineType,
    string Status,
    DateTime CapturedAtUtc,
    DateTime ReceivedAtUtc,
    bool HasSkewWarning,
    IReadOnlyList<int> PhotoAttachmentIds,
    string? WarningsJson,
    string? RejectionReason,
    bool HasReuseWarning,
    object Values);          // typed per line type below

public sealed record ReviewMeterLineDto(
    int Id,
    string Status,
    MeterReadingSubjectType SubjectType,
    int SubjectId,
    MeterReadingKind Kind,
    DateTime CapturedAtUtc,
    DateTime ReceivedAtUtc,
    DateOnly BusinessDate,
    bool HasSkewWarning,
    IReadOnlyList<int> PhotoAttachmentIds,
    decimal RawValue,
    decimal MultiplierSnapshot,
    bool IsMeterReset,
    string? ResetNote,
    string? WarningsJson,
    string? RejectionReason,
    decimal? PreviousApprovedValue,
    decimal? DeltaPreview,
    int? DeltaSpanDays);

// ── Reject action ─────────────────────────────────────────────────────────────

public sealed record RejectLineDto(string Reason);

// ── Meter delta ───────────────────────────────────────────────────────────────

public sealed record MeterDeltaDto(
    DateOnly BusinessDate,
    decimal? Delta,
    decimal? PreviousValue,
    decimal CurrentValue,
    int? SpanDays,
    bool IsMeterReset);

// ── Energy balance ────────────────────────────────────────────────────────────

public sealed record EnergyBalanceDto(
    int StationId,
    DateOnly Date,
    decimal ImportKwh,
    decimal GeneratorImportKwh,
    decimal SolarImportKwh,
    decimal ExportKwh,
    decimal? InternalConsumptionKwh,
    bool IsInternalMeasured,
    decimal UnexplainedDiffKwh,
    decimal UnexplainedDiffPct,
    decimal? SolarSharePct,
    bool ThresholdExceeded,
    string UnexplainedLabel);
