using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Domain.Models;

/// <summary>
/// Defines a user's data access scope using hierarchical levels.
/// null fields mean "All" at that level.
/// 
/// Hierarchy: Branch → Station → SubStation
/// Generators, FuelTanks, OilTanks, Meters are accessed automatically through their SubStation.
/// 
/// Examples:
///   BranchId=null, StationId=null, SubStationId=null → Admin (All)
///   BranchId=5,    StationId=null, SubStationId=null → All under Branch 5
///   BranchId=5,    StationId=10,  SubStationId=null → All under Station 10
///   BranchId=5,    StationId=10,  SubStationId=3   → Only SubStation 3
/// </summary>
public class UserScope : BaseEntity
{
    public int UserId { get; set; }

    /// <summary>
    /// null = All branches (Admin), specific ID = access only to that branch
    /// </summary>
    public int? BranchId { get; set; }

    /// <summary>
    /// null = All stations under BranchId, specific ID = access only to that station
    /// </summary>
    public int? StationId { get; set; }

    /// <summary>
    /// null = All sub-stations under StationId, specific ID = access only to that sub-station
    /// </summary>
    public int? SubStationId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Branch? Branch { get; set; }
    public Station? Station { get; set; }
    public SubStation? SubStation { get; set; }
}
