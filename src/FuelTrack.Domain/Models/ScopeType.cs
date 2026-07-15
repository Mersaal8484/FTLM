namespace FuelTrack.Domain.Models;

/// <summary>
/// Defines the scope type for a module.
/// Used to enforce UserScope filtering on API calls.
/// </summary>
public enum ScopeType
{
    /// <summary>No scope filtering required</summary>
    None = 0,
    
    /// <summary>Branch-level scope required</summary>
    Branch = 1,
    
    /// <summary>Station-level scope required</summary>
    Station = 2,
    
    /// <summary>SubStation-level scope required</summary>
    SubStation = 3
}
