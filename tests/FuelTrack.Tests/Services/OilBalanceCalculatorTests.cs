using FuelTrack.Application.Services;

namespace FuelTrack.Tests.Services;

public sealed class OilBalanceCalculatorTests
{
    [Fact]
    public void CalculateClosing_AppliesEveryMovementWithCorrectSign()
    {
        var closing = OilBalanceCalculator.CalculateClosing(100m, 50m, 10m, 20m, 15m, 5m);

        Assert.Equal(140m, closing);
    }

    [Fact]
    public void CalculateClosing_RoundsToThreeDecimalPlacesAwayFromZero()
    {
        var closing = OilBalanceCalculator.CalculateClosing(0m, 1.2345m, 0m, 0m, 0m, 0m);

        Assert.Equal(1.235m, closing);
    }

    [Theory]
    [InlineData("Purchase", true, false)]
    [InlineData("TransferIn", true, false)]
    [InlineData("Sale", false, true)]
    [InlineData("TransferOut", false, true)]
    [InlineData("Loss", false, true)]
    public void MovementDirection_IsClassifiedByTypeCode(string code, bool inbound, bool outbound)
    {
        Assert.Equal(inbound, OilBalanceCalculator.IsInbound(code));
        Assert.Equal(outbound, OilBalanceCalculator.IsOutbound(code));
    }
}
