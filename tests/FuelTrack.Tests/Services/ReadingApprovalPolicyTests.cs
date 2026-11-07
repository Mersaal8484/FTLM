using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Services;

namespace FuelTrack.Tests.Services;

public sealed class ReadingApprovalPolicyTests
{
    [Theory]
    [InlineData(OperationalSubjectType.Generator, false)]
    [InlineData(OperationalSubjectType.Feeder, false)]
    public void TierOneMonitoring_DoesNotRequireApproval(OperationalSubjectType subject, bool expected) =>
        Assert.Equal(expected, ReadingApprovalPolicy.RequiresApproval(subject));

    [Fact]
    public void DailyClosing_RequiresApproval() =>
        Assert.True(ReadingApprovalPolicy.RequiresApproval(OperationalSubjectType.Meter, MeterReadingKind.DailyClosing));

    [Fact]
    public void RoutineTankLevel_IsRecorded_AndOfficial()
    {
        var status = ReadingApprovalPolicy.InitialStatus(ReadingApprovalPolicy.RequiresApproval(OperationalSubjectType.Tank, eventType: TankEventType.Level), false);
        Assert.Equal(OperationalReadingStatus.Recorded, status);
        Assert.True(ReadingApprovalPolicy.IsOfficial(status));
    }

    [Theory]
    [InlineData(TankEventType.Receipt)]
    [InlineData(TankEventType.TransferIn)]
    [InlineData(TankEventType.TransferOut)]
    public void TankMovements_RequireApproval(TankEventType eventType) =>
        Assert.True(ReadingApprovalPolicy.RequiresApproval(OperationalSubjectType.Tank, eventType: eventType));

    [Fact]
    public void AwaitingPhoto_IsNeverOfficial() =>
        Assert.False(ReadingApprovalPolicy.IsOfficial(OperationalReadingStatus.AwaitingPhoto));
}
