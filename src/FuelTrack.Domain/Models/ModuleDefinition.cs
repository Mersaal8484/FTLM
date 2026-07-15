namespace FuelTrack.Domain.Models;

/// <summary>
/// Represents a page/module in the system.
/// This is the Single Source of Truth for all permissions, menu items, and scope enforcement.
/// </summary>
public class ModuleDefinition
{
    public int Id { get; set; }
    
    /// <summary>Unique key for the module (e.g., "generators", "daily_readings")</summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>Display name in English</summary>
    public string NameEn { get; set; } = string.Empty;
    
    /// <summary>Display name in Arabic</summary>
    public string NameAr { get; set; } = string.Empty;
    
    /// <summary>Route path (e.g., "/generators")</summary>
    public string Route { get; set; } = string.Empty;
    
    /// <summary>Icon name (e.g., "Zap", "BookOpen")</summary>
    public string Icon { get; set; } = string.Empty;
    
    /// <summary>Parent module key (null for top-level)</summary>
    public string? ParentKey { get; set; }
    
    /// <summary>Permission template to use</summary>
    public ModuleTemplate Template { get; set; }
    
    /// <summary>Scope type for this module</summary>
    public ScopeType ScopeType { get; set; }
    
    /// <summary>Comma-separated list of custom actions (e.g., "reset_password,send")</summary>
    public string? CustomActions { get; set; }
    
    /// <summary>Sort order in the menu</summary>
    public int SortOrder { get; set; }
    
    /// <summary>Whether this module is visible in the sidebar</summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>Whether this module is active</summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>Whether this module supports audit logging</summary>
    public bool SupportsAuditLog { get; set; }
    
    /// <summary>Whether this module appears in global search</summary>
    public bool SupportsGlobalSearch { get; set; }
    
    /// <summary>Navigation property to parent</summary>
    public ModuleDefinition? Parent { get; set; }
    
    /// <summary>Navigation property to children</summary>
    public ICollection<ModuleDefinition> Children { get; set; } = new List<ModuleDefinition>();
    
    /// <summary>Navigation property to generated permissions</summary>
    public ICollection<PagePermission> PagePermissions { get; set; } = new List<PagePermission>();
}
