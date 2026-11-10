namespace FuelTrack.Domain.Models;

/// <summary>
/// Operational policy for a station. Values are intentionally independent from
/// a particular customer's terminology or workflow implementation.
/// </summary>
public sealed class StationOperationalSettings : BaseEntity
{
    public const string DefaultTimeZone = "Asia/Aden";
    public const string DefaultOilPressureUnit = "bar";
    public const int DefaultReadingPhotoMaxBytes = 65 * 1024;
    public const int DefaultMissingDataIntervalMinutes = 60;
    public const decimal DefaultAverageLoadWarningTolerancePercent = 15m;
    public const decimal DefaultFuelReconciliationWarningThresholdPercent = 10m;
    public const int DefaultRepeatedValueWarningCount = 6;

    public int StationId { get; set; }
    public string TimeZone { get; set; } = DefaultTimeZone;
    public decimal ProducedVsSentWarningThresholdPercent { get; set; } = 10m;
    public string OilPressureUnit { get; set; } = DefaultOilPressureUnit;
    public bool IsReadingPhotoRequired { get; set; } = true;
    public int ReadingPhotoMaxBytes { get; set; } = DefaultReadingPhotoMaxBytes;
    public bool RequiresDifferentReviewer { get; set; } = true;
    /// <summary>Exceptional station policy that sends monitoring lines through review.</summary>
    public bool Tier1RequiresApproval { get; set; }
    /// <summary>
    /// Optional meter subject used as the internal-consumption source for the energy balance (§7.5).
    /// When null the unexplained difference is labelled "غير مفسَّر (يشمل الاستهلاك الداخلي)"
    /// and the warning threshold is suppressed.
    /// </summary>
    public int? InternalConsumptionMeterId { get; set; }
    public decimal OverloadThresholdPercent { get; set; } = 100m;
    public int OverloadHysteresisReadings { get; set; } = 3;
    /// <summary>Expected Tier-1 cadence used only to expose missing operational slots.</summary>
    public int MissingDataIntervalMinutes { get; set; } = DefaultMissingDataIntervalMinutes;
    public decimal AverageLoadWarningTolerancePercent { get; set; } = DefaultAverageLoadWarningTolerancePercent;
    public decimal FuelReconciliationWarningThresholdPercent { get; set; } = DefaultFuelReconciliationWarningThresholdPercent;
    /// <summary>Consecutive equal active-load values required before recording a consistency warning.</summary>
    public int RepeatedValueWarningCount { get; set; } = DefaultRepeatedValueWarningCount;

    public Station Station { get; set; } = null!;
}
