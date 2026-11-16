namespace FuelTrack.Domain.Enums;

/// <summary>
/// Commercial-mode tank role.
/// Main  (1): the primary horizontal main storage tank at the commercial station.
/// Daily (2): the daily service tank linked to a specific generator (via GeneratorId).
/// Null column value means enterprise mode — no role constraint applied.
/// </summary>
public enum TankRole
{
    Main  = 1,
    Daily = 2,
}
