namespace FuelTrack.Domain.Models;

/// <summary>
/// Defines the permission template for a module.
/// Used to auto-generate permissions from the Module Registry.
/// </summary>
public enum ModuleTemplate
{
    /// <summary>Standard CRUD: View, Create, Update, Delete</summary>
    CRUD = 0,
    
    /// <summary>Approval workflow: View, Create, Update, Delete, Approve, Reject</summary>
    Approval = 1,
    
    /// <summary>Report: View, Print, Export</summary>
    Report = 2,
    
    /// <summary>Dashboard: View only</summary>
    Dashboard = 3,
    
    /// <summary>Settings: Full access (View, Create, Update, Delete)</summary>
    Settings = 4,
    
    /// <summary>Read-only: View only</summary>
    ReadOnly = 5
}
