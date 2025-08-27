using MauiApp1.Models;

namespace MauiApp1.Services;

/// <summary>
/// Service for handling user authorization and role-based access control
/// </summary>
public interface IAuthorizationService
{
    // Core AD integration
    Task<bool> IsInGroupAsync(string groupName);
    Task<List<string>> GetUserGroupsAsync();
    Task<string> GetCurrentUserNameAsync();
    Task<string> GetCurrentDomainAsync();
    
    // Role-based access
    Task<UserRole> GetUserRoleAsync();
    Task<Permission> GetUserPermissionsAsync();
    Task<bool> HasPermissionAsync(Permission permission);
    
    // Specific capability checks
    Task<bool> CanAccessDatabaseAdminAsync();
    Task<bool> CanEditDevicesDirectlyAsync();
    Task<bool> CanApproveChangesAsync();
    Task<bool> CanAccessSystemSettingsAsync();
    
    // Database connection restrictions
    Task<bool> CanConnectToDatabaseAsync(string serverName);
    Task<bool> IsNonDomainEnvironmentAsync();
    
    // Developer mode support
    bool IsDeveloperModeEnabled { get; }
    UserRole? SimulatedRole { get; }
    void SetDeveloperMode(bool enabled, UserRole? simulatedRole = null);
    
    // Cache management
    void ClearCache();
    
    // Configuration
    Task<AuthorizationConfig> GetConfigurationAsync();
    Task SaveConfigurationAsync(AuthorizationConfig config);
}

/// <summary>
/// Configuration for authorization settings
/// </summary>
public class AuthorizationConfig
{
    public string DomainName { get; set; } = "PLACEHOLDER_DOMAIN";
    public bool EnableDeveloperMode { get; set; } = true; // Only in debug builds
    public int CacheTimeoutMinutes { get; set; } = 30;
}