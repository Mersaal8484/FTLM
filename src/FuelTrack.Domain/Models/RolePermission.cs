namespace FuelTrack.Domain.Models;

/// <summary>
/// Links a role to a specific page permission.
/// Replaces the old JSON-based permission storage.
/// </summary>
public class RolePermission
{
    public int Id { get; set; }
    
    /// <summary>Role ID</summary>
    public int RoleId { get; set; }
    
    /// <summary>Page permission ID</summary>
    public int PagePermissionId { get; set; }
    
    /// <summary>Whether this permission is granted</summary>
    public bool IsGranted { get; set; } = true;
    
    /// <summary>Navigation property to role</summary>
    public Role Role { get; set; } = null!;
    
    /// <summary>Navigation property to page permission</summary>
    public PagePermission PagePermission { get; set; } = null!;
}
