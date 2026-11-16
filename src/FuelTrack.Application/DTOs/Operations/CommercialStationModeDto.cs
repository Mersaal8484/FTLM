namespace FuelTrack.Application.DTOs.Operations;

/// <summary>
/// Deliberately exposes the operating mode, but never the hidden technical
/// hierarchy itself to commercial-mode clients.
/// </summary>
public sealed record CommercialStationModeDto(bool IsCommercialMode);

public sealed record CommercialStationActivationPreflightDto(
    bool CanActivate,
    string? BlockingReason,
    int ActiveBranchCount,
    int ActiveStationCount,
    int ActiveSubStationCount);

/// <summary>
/// Activation is explicit because the station code becomes the stable prefix
/// used by later commercial workflows (for example work permits).
/// </summary>
public sealed record ActivateCommercialStationModeDto(string StationName, string StationCode);

/// <summary>Internal server-side resolution of the hidden commercial scope.</summary>
public sealed record CommercialStationScope(int BranchId, int StationId, int SubStationId);
