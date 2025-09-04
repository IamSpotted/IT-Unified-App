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
    
    // Testing mode support (TODO: Remove before production)
    void EnableTestingMode(bool enabled = true);
    
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
    
    /// <summary>
    /// Approved database servers that users can connect to (in addition to localhost for non-domain)
    /// Empty list means any server is allowed for domain users with DatabaseAdmin permission
    /// </summary>
    public List<string> ApprovedDatabaseServers { get; set; } = new List<string>();
    
    /// <summary>
    /// Whether to enforce the approved database servers list for domain users
    /// </summary>
    public bool EnforceApprovedServersOnly { get; set; } = false;
}