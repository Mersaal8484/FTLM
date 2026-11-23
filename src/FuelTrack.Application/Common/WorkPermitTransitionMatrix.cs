namespace FuelTrack.Application.Common;

/// <summary>
/// Pure transition matrix defining valid status transitions and permission gates
/// for work permits in commercial mode.
/// </summary>
public static class WorkPermitTransitionMatrix
{
    public const string ActionRequest = "create";
    public const string ActionReviewAssign = "update";
    public const string ActionApprove = "approve";
    public const string ActionClose = "close_permit";

    public static readonly IReadOnlySet<string> TerminalStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Completed",
        "Cancelled"
    };

    public sealed record TransitionRule(
        string FromStatus,
        string ToStatus,
        string RequiredActionKey,
        string Description);

    private static readonly List<TransitionRule> Rules = new()
    {
        // Operator requests / submits permit
        new("Draft", "Submitted", "create", "Operator submit permit request"),
        new("Draft", "Cancelled", "update", "Cancel draft permit"),

        // Operations Manager approves or rejects
        new("Submitted", "Approved", "approve", "Operations Manager approve permit"),
        new("Submitted", "Rejected", "approve", "Operations Manager reject permit"),
        new("Submitted", "Cancelled", "update", "Cancel submitted permit"),

        // Maintenance starts work (activates)
        new("Approved", "Active", "update", "Maintenance start work / activate permit"),
        new("Approved", "Cancelled", "update", "Cancel approved permit"),

        // Maintenance completes work
        new("Active", "Completed", "update", "Maintenance complete work"),
        new("Active", "Cancelled", "update", "Cancel active permit"),

        // Reopen or cancel rejected permit
        new("Rejected", "Draft", "update", "Reopen rejected permit as draft"),
        new("Rejected", "Cancelled", "update", "Cancel rejected permit")
    };

    public static bool IsTerminal(string status) =>
        TerminalStatuses.Contains(status);

    public static bool IsTransitionAllowed(
        string fromStatus,
        string toStatus,
        out string? requiredActionKey,
        out string? errorMessage)
    {
        if (IsTerminal(fromStatus))
        {
            requiredActionKey = null;
            errorMessage = $"Status '{fromStatus}' is terminal and cannot transition to any other status.";
            return false;
        }

        var match = Rules.FirstOrDefault(r =>
            string.Equals(r.FromStatus, fromStatus, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.ToStatus, toStatus, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            requiredActionKey = null;
            errorMessage = $"Transition from '{fromStatus}' to '{toStatus}' is invalid.";
            return false;
        }

        requiredActionKey = match.RequiredActionKey;
        errorMessage = null;
        return true;
    }

    public static IReadOnlyList<TransitionRule> GetAllRules() => Rules;
}
