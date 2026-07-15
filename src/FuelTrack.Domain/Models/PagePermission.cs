namespace FuelTrack.Domain.Models;

/// <summary>
/// Represents a permission for a specific module and action.
/// Auto-generated from the Module Registry.
/// </summary>
public class PagePermission
{
    public int Id { get; set; }
    
    /// <summary>Module definition ID</summary>
    public int ModuleDefinitionId { get; set; }
    
    /// <summary>Action key (e.g., "view", "create", "update", "delete", "approve", "reject", "print", "export")</summary>
    public string ActionKey { get; set; } = string.Empty;
    
    /// <summary>Display name in English</summary>
    public string NameEn { get; set; } = string.Empty;
    
    /// <summary>Display name in Arabic</summary>
    public string NameAr { get; set; } = string.Empty;
    
    /// <summary>Sort order</summary>
    public int SortOrder { get; set; }
    
    /// <summary>Whether this permission is active</summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>Navigation property to module</summary>
    public ModuleDefinition ModuleDefinition { get; set; } = null!;
}
