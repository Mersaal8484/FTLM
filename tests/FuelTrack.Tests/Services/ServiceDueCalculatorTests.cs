using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Services;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class ServiceDueCalculatorTests
{
    [Theory]
    [InlineData("Perkins 4012", "PERKINS 4012")]
    [InlineData("  cummins   qsk60  ", "CUMMINS QSK60")]
    [InlineData("CAT-3516B", "CAT-3516B")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void GeneratorModelKey_NormalizesCorrectly(string? input, string expected)
    {
        var result = GeneratorModelKey.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LeadHours_DefaultsToMaxOf50Or10Percent()
    {
        // 250 h: 10% = 25, max(50, 25) = 50
        Assert.Equal(50m, ServiceDueCalculator.CalculateLeadHours(250m));

        // 500 h: 10% = 50, max(50, 50) = 50
        Assert.Equal(50m, ServiceDueCalculator.CalculateLeadHours(500m));

        // 1000 h: 10% = 100, max(50, 100) = 100
        Assert.Equal(100m, ServiceDueCalculator.CalculateLeadHours(1000m));

        // Custom lead hours override
        Assert.Equal(75m, ServiceDueCalculator.CalculateLeadHours(250m, 75m));
    }

    [Fact]
    public void Calculate_NotBaselined_WhenNoLastServiceHours()
    {
        var result = ServiceDueCalculator.Calculate(
            lastServiceHours: null,
            intervalHours: 250m,
            currentHours: 1500m);

        Assert.Equal(ServiceDueStatus.NotBaselined, result.Status);
        Assert.Null(result.RemainingHours);
        Assert.Null(result.DueAtHours);
        Assert.Equal(0m, result.ProgressFraction);
    }

    [Fact]
    public void Calculate_NotBaselined_WhenMeterResetAfterLastService()
    {
        var result = ServiceDueCalculator.Calculate(
            lastServiceHours: 8500m,
            intervalHours: 250m,
            currentHours: 120m,
            isMeterResetAfterLastService: true);

        Assert.Equal(ServiceDueStatus.NotBaselined, result.Status);
        Assert.Contains("reset", result.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_Ok_WhenRemainingGreaterThanLeadHours()
    {
        // Last service at 1000 h, interval 250 h, due at 1250 h.
        // Lead hours = max(50, 25) = 50 h.
        // Current = 1150 h -> remaining = 100 h (> 50) -> Ok
        var result = ServiceDueCalculator.Calculate(
            lastServiceHours: 1000m,
            intervalHours: 250m,
            currentHours: 1150m);

        Assert.Equal(ServiceDueStatus.Ok, result.Status);
        Assert.Equal(100m, result.RemainingHours);
        Assert.Equal(1250m, result.DueAtHours);
        Assert.Equal(0m, result.OverdueByHours);
        Assert.Equal(150m / 250m, result.ProgressFraction);
    }

    [Fact]
    public void Calculate_DueSoon_WhenRemainingWithinLeadHours()
    {
        // Current = 1210 h -> remaining = 40 h (<= 50) -> DueSoon
        var result = ServiceDueCalculator.Calculate(
            lastServiceHours: 1000m,
            intervalHours: 250m,
            currentHours: 1210m);

        Assert.Equal(ServiceDueStatus.DueSoon, result.Status);
        Assert.Equal(40m, result.RemainingHours);
        Assert.Equal(0m, result.OverdueByHours);
    }

    [Fact]
    public void Calculate_DueSoon_ExactlyAtLeadBoundary()
    {
        // Current = 1200 h -> remaining = 50 h (exactly equal to lead) -> DueSoon
        var result = ServiceDueCalculator.Calculate(
            lastServiceHours: 1000m,
            intervalHours: 250m,
            currentHours: 1200m);

        Assert.Equal(ServiceDueStatus.DueSoon, result.Status);
        Assert.Equal(50m, result.RemainingHours);
    }

    [Fact]
    public void Calculate_DueSoon_OneHourBeforeDue()
    {
        // Current = 1249 h -> remaining = 1 h -> DueSoon
        var result = ServiceDueCalculator.Calculate(
            lastServiceHours: 1000m,
            intervalHours: 250m,
            currentHours: 1249m);

        Assert.Equal(ServiceDueStatus.DueSoon, result.Status);
        Assert.Equal(1m, result.RemainingHours);
    }

    [Fact]
    public void Calculate_Due_ExactlyAtInterval()
    {
        // Current = 1250 h -> remaining = 0 h -> Due
        var result = ServiceDueCalculator.Calculate(
            lastServiceHours: 1000m,
            intervalHours: 250m,
            currentHours: 1250m);

        Assert.Equal(ServiceDueStatus.Due, result.Status);
        Assert.Equal(0m, result.RemainingHours);
        Assert.Equal(0m, result.OverdueByHours);
        Assert.Equal(1m, result.ProgressFraction);
    }

    [Fact]
    public void Calculate_Overdue_OneHourAfterDue()
    {
        // Current = 1251 h -> remaining = -1 h -> Overdue by 1 h
        var result = ServiceDueCalculator.Calculate(
            lastServiceHours: 1000m,
            intervalHours: 250m,
            currentHours: 1251m);

        Assert.Equal(ServiceDueStatus.Overdue, result.Status);
        Assert.Equal(-1m, result.RemainingHours);
        Assert.Equal(1m, result.OverdueByHours);
    }

    [Fact]
    public void Calculate_Overdue_MultipleHoursOverdue()
    {
        // Current = 1350 h -> overdue by 100 h
        var result = ServiceDueCalculator.Calculate(
            lastServiceHours: 1000m,
            intervalHours: 250m,
            currentHours: 1350m);

        Assert.Equal(ServiceDueStatus.Overdue, result.Status);
        Assert.Equal(-100m, result.RemainingHours);
        Assert.Equal(100m, result.OverdueByHours);
        Assert.Equal(350m / 250m, result.ProgressFraction);
    }

    [Fact]
    public void ValidateReplacementHours_NegativeHours_Fails()
    {
        var result = ServiceDueCalculator.ValidateReplacementHours(
            newHours: -5m,
            previousHours: null,
            intervalHours: 250m,
            kind: ServiceRecordKind.Replacement,
            earlyReason: null);

        Assert.False(result.IsValid);
        Assert.Contains("negative", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateReplacementHours_RegressedHours_Fails()
    {
        // Previous = 2000 h, New = 1950 h without reset -> fails
        var result = ServiceDueCalculator.ValidateReplacementHours(
            newHours: 1950m,
            previousHours: 2000m,
            intervalHours: 250m,
            kind: ServiceRecordKind.Replacement,
            earlyReason: null,
            isResetAfterPrevious: false);

        Assert.False(result.IsValid);
        Assert.Contains("cannot be less", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateReplacementHours_RegressedHours_AllowedIfMeterReset()
    {
        // Previous = 9500 h, New = 50 h WITH reset -> allowed!
        var result = ServiceDueCalculator.ValidateReplacementHours(
            newHours: 50m,
            previousHours: 9500m,
            intervalHours: 250m,
            kind: ServiceRecordKind.Replacement,
            earlyReason: null,
            isResetAfterPrevious: true);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateReplacementHours_EarlyReplacement_RequiresReason()
    {
        // Previous = 1000 h, interval = 250 h. New = 1100 h (delta = 100 < 250).
        // No early reason provided -> fails!
        var result = ServiceDueCalculator.ValidateReplacementHours(
            newHours: 1100m,
            previousHours: 1000m,
            intervalHours: 250m,
            kind: ServiceRecordKind.Replacement,
            earlyReason: null);

        Assert.False(result.IsValid);
        Assert.True(result.IsEarly);
        Assert.Contains("reason is required", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateReplacementHours_EarlyReplacement_WithReason_Passes()
    {
        // With reason -> passes
        var result = ServiceDueCalculator.ValidateReplacementHours(
            newHours: 1100m,
            previousHours: 1000m,
            intervalHours: 250m,
            kind: ServiceRecordKind.Replacement,
            earlyReason: "Damaged oil filter during routine check");

        Assert.True(result.IsValid);
        Assert.True(result.IsEarly);
    }

    [Fact]
    public void ValidateReplacementHours_NormalReplacement_Passes()
    {
        // Delta = 260 >= 250 -> normal replacement, no reason required
        var result = ServiceDueCalculator.ValidateReplacementHours(
            newHours: 1260m,
            previousHours: 1000m,
            intervalHours: 250m,
            kind: ServiceRecordKind.Replacement,
            earlyReason: null);

        Assert.True(result.IsValid);
        Assert.False(result.IsEarly);
    }

    [Fact]
    public void ValidateReplacementHours_Baseline_NeverEarly()
    {
        var result = ServiceDueCalculator.ValidateReplacementHours(
            newHours: 500m,
            previousHours: 480m,
            intervalHours: 250m,
            kind: ServiceRecordKind.Baseline,
            earlyReason: null);

        Assert.True(result.IsValid);
        Assert.False(result.IsEarly);
    }

    [Fact]
    public void AlertDeduplication_ShouldNotify_OnUpwardEscalationOnly()
    {
        const int cycle1 = 101;

        // 1. Initial Ok: do not notify
        Assert.False(ServiceDueCalculator.ShouldNotify(null, ServiceDueStatus.Ok, cycle1, cycle1));

        // 2. Escalation to DueSoon: notify!
        Assert.True(ServiceDueCalculator.ShouldNotify(null, ServiceDueStatus.DueSoon, cycle1, cycle1));

        // 3. Repeated evaluation at DueSoon: duplicate -> do not notify
        Assert.False(ServiceDueCalculator.ShouldNotify(ServiceDueStatus.DueSoon, ServiceDueStatus.DueSoon, cycle1, cycle1));

        // 4. Escalation to Due: notify!
        Assert.True(ServiceDueCalculator.ShouldNotify(ServiceDueStatus.DueSoon, ServiceDueStatus.Due, cycle1, cycle1));

        // 5. Repeated evaluation at Due: duplicate -> do not notify
        Assert.False(ServiceDueCalculator.ShouldNotify(ServiceDueStatus.Due, ServiceDueStatus.Due, cycle1, cycle1));

        // 6. Escalation to Overdue: notify!
        Assert.True(ServiceDueCalculator.ShouldNotify(ServiceDueStatus.Due, ServiceDueStatus.Overdue, cycle1, cycle1));

        // 7. Repeated evaluation at Overdue: duplicate -> do not notify
        Assert.False(ServiceDueCalculator.ShouldNotify(ServiceDueStatus.Overdue, ServiceDueStatus.Overdue, cycle1, cycle1));

        // 8. Service performed -> New cycle (cycle2 = 102), state returns to Ok
        const int cycle2 = 102;
        // Never notify on return to Ok!
        Assert.False(ServiceDueCalculator.ShouldNotify(ServiceDueStatus.Overdue, ServiceDueStatus.Ok, cycle1, cycle2));

        // 9. In new cycle, reaches DueSoon: notify!
        Assert.True(ServiceDueCalculator.ShouldNotify(null, ServiceDueStatus.DueSoon, cycle2, cycle2));
    }
}
