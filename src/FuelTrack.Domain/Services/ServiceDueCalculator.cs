using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Services;

public sealed record ServiceDueCalculationResult(
    ServiceDueStatus Status,
    decimal? RemainingHours,
    decimal OverdueByHours,
    decimal? DueAtHours,
    decimal LeadHours,
    decimal ProgressFraction,
    string? Reason = null);

public sealed record ServiceReplacementValidationResult(
    bool IsValid,
    bool IsEarly,
    string? ErrorMessage = null);

/// <summary>
/// Pure due calculator and business validator for generator service intervals.
/// Encapsulates strict status boundaries (Ok, DueSoon, Due, Overdue, NotBaselined),
/// early replacement checks with mandatory reason, and alert deduplication rules.
/// </summary>
public static class ServiceDueCalculator
{
    public static decimal CalculateLeadHours(decimal intervalHours, decimal? customLeadHours = null)
    {
        if (customLeadHours.HasValue && customLeadHours.Value > 0)
        {
            return customLeadHours.Value;
        }

        // Default lead hours: max(50, 10% of interval)
        return Math.Max(50m, intervalHours * 0.10m);
    }

    public static ServiceDueCalculationResult Calculate(
        decimal? lastServiceHours,
        decimal intervalHours,
        decimal currentHours,
        decimal? warningLeadHours = null,
        bool isMeterResetAfterLastService = false)
    {
        var leadHours = CalculateLeadHours(intervalHours, warningLeadHours);

        if (isMeterResetAfterLastService)
        {
            return new ServiceDueCalculationResult(
                ServiceDueStatus.NotBaselined,
                RemainingHours: null,
                OverdueByHours: 0m,
                DueAtHours: null,
                LeadHours: leadHours,
                ProgressFraction: 0m,
                Reason: "Hour meter was reset since last service; rebaseline required.");
        }

        if (!lastServiceHours.HasValue)
        {
            return new ServiceDueCalculationResult(
                ServiceDueStatus.NotBaselined,
                RemainingHours: null,
                OverdueByHours: 0m,
                DueAtHours: null,
                LeadHours: leadHours,
                ProgressFraction: 0m,
                Reason: "No service baseline recorded for this item.");
        }

        var hoursSinceService = currentHours - lastServiceHours.Value;
        var dueAt = lastServiceHours.Value + intervalHours;
        var remaining = intervalHours - hoursSinceService;
        var progress = intervalHours > 0m ? (hoursSinceService / intervalHours) : 1m;

        if (remaining < 0m)
        {
            return new ServiceDueCalculationResult(
                ServiceDueStatus.Overdue,
                RemainingHours: remaining,
                OverdueByHours: Math.Abs(remaining),
                DueAtHours: dueAt,
                LeadHours: leadHours,
                ProgressFraction: progress);
        }

        if (remaining == 0m)
        {
            return new ServiceDueCalculationResult(
                ServiceDueStatus.Due,
                RemainingHours: 0m,
                OverdueByHours: 0m,
                DueAtHours: dueAt,
                LeadHours: leadHours,
                ProgressFraction: 1m);
        }

        if (remaining <= leadHours)
        {
            return new ServiceDueCalculationResult(
                ServiceDueStatus.DueSoon,
                RemainingHours: remaining,
                OverdueByHours: 0m,
                DueAtHours: dueAt,
                LeadHours: leadHours,
                ProgressFraction: progress);
        }

        return new ServiceDueCalculationResult(
            ServiceDueStatus.Ok,
            RemainingHours: remaining,
            OverdueByHours: 0m,
            DueAtHours: dueAt,
            LeadHours: leadHours,
            ProgressFraction: progress);
    }

    public static ServiceReplacementValidationResult ValidateReplacementHours(
        decimal newHours,
        decimal? previousHours,
        decimal intervalHours,
        ServiceRecordKind kind,
        string? earlyReason,
        bool isResetAfterPrevious = false)
    {
        if (newHours < 0m)
        {
            return new ServiceReplacementValidationResult(false, false, "Running hours cannot be negative.");
        }

        if (previousHours.HasValue && !isResetAfterPrevious)
        {
            if (newHours < previousHours.Value)
            {
                return new ServiceReplacementValidationResult(
                    false,
                    false,
                    $"New service hours ({newHours:0.##}) cannot be less than previous service hours ({previousHours.Value:0.##}).");
            }

            var delta = newHours - previousHours.Value;
            var isEarly = kind == ServiceRecordKind.Replacement && delta < intervalHours;

            if (isEarly && string.IsNullOrWhiteSpace(earlyReason))
            {
                return new ServiceReplacementValidationResult(
                    false,
                    true,
                    $"Early replacement detected ({delta:0.##} hours since previous service, interval is {intervalHours:0.##} hours). A reason is required.");
            }

            return new ServiceReplacementValidationResult(true, isEarly);
        }

        return new ServiceReplacementValidationResult(true, false);
    }

    public static bool ShouldNotify(
        ServiceDueStatus? previousHighest,
        ServiceDueStatus currentStatus,
        int? previousCycleRecordId,
        int? currentCycleRecordId)
    {
        // Only DueSoon, Due, and Overdue trigger notifications
        if (currentStatus is not (ServiceDueStatus.DueSoon or ServiceDueStatus.Due or ServiceDueStatus.Overdue))
        {
            return false;
        }

        // If cycle changed (e.g. fresh replacement/baseline), evaluate fresh
        if (currentCycleRecordId != previousCycleRecordId)
        {
            return true;
        }

        // Same cycle: notify only on upward escalation
        if (!previousHighest.HasValue)
        {
            return true;
        }

        return (int)currentStatus > (int)previousHighest.Value;
    }
}
