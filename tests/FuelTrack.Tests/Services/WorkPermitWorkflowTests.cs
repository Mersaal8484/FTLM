using FuelTrack.Application.Common;
using Xunit;

namespace FuelTrack.Tests.Services;

public class WorkPermitWorkflowTests
{
    [Theory]
    [InlineData("Draft", "Submitted", "create")]
    [InlineData("Draft", "Cancelled", "update")]
    [InlineData("Submitted", "Approved", "approve")]
    [InlineData("Submitted", "Rejected", "approve")]
    [InlineData("Submitted", "Cancelled", "update")]
    [InlineData("Approved", "Active", "update")]
    [InlineData("Approved", "Cancelled", "update")]
    [InlineData("Active", "Completed", "update")]
    [InlineData("Active", "Cancelled", "update")]
    [InlineData("Rejected", "Draft", "update")]
    [InlineData("Rejected", "Cancelled", "update")]
    public void AllowedTransitions_ReturnTrue_WithExpectedRequiredAction(string from, string to, string expectedAction)
    {
        var allowed = WorkPermitTransitionMatrix.IsTransitionAllowed(from, to, out var action, out var error);

        Assert.True(allowed);
        Assert.Equal(expectedAction, action);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("Draft", "Completed")]
    [InlineData("Draft", "Active")]
    [InlineData("Draft", "Approved")]
    [InlineData("Submitted", "Completed")]
    [InlineData("Submitted", "Active")]
    [InlineData("Approved", "Completed")]
    [InlineData("Rejected", "Active")]
    [InlineData("Rejected", "Completed")]
    public void InvalidTransitions_ReturnFalse_WithErrorMessage(string from, string to)
    {
        var allowed = WorkPermitTransitionMatrix.IsTransitionAllowed(from, to, out var action, out var error);

        Assert.False(allowed);
        Assert.Null(action);
        Assert.NotNull(error);
        Assert.Contains("invalid", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Completed", "Draft")]
    [InlineData("Completed", "Submitted")]
    [InlineData("Completed", "Active")]
    [InlineData("Completed", "Cancelled")]
    [InlineData("Cancelled", "Draft")]
    [InlineData("Cancelled", "Submitted")]
    [InlineData("Cancelled", "Active")]
    [InlineData("Cancelled", "Completed")]
    public void TerminalStatuses_CannotTransition_ReturnsTerminalError(string terminalStatus, string to)
    {
        Assert.True(WorkPermitTransitionMatrix.IsTerminal(terminalStatus));

        var allowed = WorkPermitTransitionMatrix.IsTransitionAllowed(terminalStatus, to, out var action, out var error);

        Assert.False(allowed);
        Assert.Null(action);
        Assert.NotNull(error);
        Assert.Contains("terminal", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllowedTransitions_AreCaseInsensitive()
    {
        var allowed = WorkPermitTransitionMatrix.IsTransitionAllowed("draft", "SUBMITTED", out var action, out var error);

        Assert.True(allowed);
        Assert.Equal("create", action);
        Assert.Null(error);
    }
}
