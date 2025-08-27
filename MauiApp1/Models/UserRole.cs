namespace MauiApp1.Models;

/// <summary>
/// Defines the user roles available in the application
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Can only view devices and data - no edit permissions
    /// </summary>
    ReadOnly = 0,
    
    /// <summary>
    /// Can edit devices but changes require approval
    /// </summary>
    Standard = 1,
    
    /// <summary>
    /// Can only configure domain settings - no database or system access
    /// </summary>
    LocalAdmin = 2,
    
    /// <summary>
    /// Full database access and can approve changes from standard users
    /// </summary>
    DatabaseAdmin = 3,
    
    /// <summary>
    /// All permissions including user management and system configuration
    /// </summary>
    SystemAdmin = 4
}

/// <summary>
/// Specific permissions that can be granted to users
/// </summary>
[Flags]
public enum Permission
{
    None = 0,
    
    // View permissions
    ViewDevices = 1 << 0,
    ViewReports = 1 << 1,
    ViewSettings = 1 << 2,
    
    // Edit permissions
    EditDevices = 1 << 3,
    EditDirectly = 1 << 4,  // Bypass approval workflow
    
    // Admin permissions
    DatabaseAdmin = 1 << 5,
    ApproveChanges = 1 << 6,
    UserManagement = 1 << 7,
    SystemConfiguration = 1 << 8,
    
    // Developer permissions
    DeveloperMode = 1 << 9
}

/// <summary>
/// Maps user roles to their permissions
/// </summary>
public static class RolePermissions
{
    public static readonly Dictionary<UserRole, Permission> RoleToPermissions = new()
    {
        [UserRole.ReadOnly] = Permission.ViewDevices | Permission.ViewReports,
        
        [UserRole.Standard] = Permission.ViewDevices | Permission.ViewReports | 
                             Permission.ViewSettings | Permission.EditDevices,
        
        [UserRole.DatabaseAdmin] = Permission.ViewDevices | Permission.ViewReports | 
                                  Permission.ViewSettings | Permission.EditDevices | 
                                  Permission.EditDirectly | Permission.DatabaseAdmin | 
                                  Permission.ApproveChanges,
        
        [UserRole.SystemAdmin] = Permission.ViewDevices | Permission.ViewReports | 
                                Permission.ViewSettings | Permission.EditDevices | 
                                Permission.EditDirectly | Permission.DatabaseAdmin | 
                                Permission.ApproveChanges | Permission.UserManagement | 
                                Permission.SystemConfiguration | Permission.DeveloperMode
    };
    
    /// <summary>
    /// Get all permissions for a given role
    /// </summary>
    public static Permission GetPermissions(UserRole role)
    {
        return RoleToPermissions.TryGetValue(role, out var permissions) ? permissions : Permission.None;
    }
    
    /// <summary>
    /// Check if a role has a specific permission
    /// </summary>
    public static bool HasPermission(UserRole role, Permission permission)
    {
        var rolePermissions = GetPermissions(role);
        return (rolePermissions & permission) == permission;
    }
}
