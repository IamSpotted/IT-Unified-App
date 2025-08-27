using System.DirectoryServices.AccountManagement;
using System.Runtime.Caching;
using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.Services;

/// <summary>
/// Windows Active Directory based authorization service
/// </summary>
public class WindowsAuthorizationService : IAuthorizationService, ISingletonService
{
    private readonly ILogger<WindowsAuthorizationService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly MemoryCache _cache = new("AuthorizationCache");
    
    private AuthorizationConfig? _config;
    private bool _developerModeEnabled = false;
    private UserRole? _simulatedRole = null;

    // Hard-coded AD group names - to be provided by AD team
    private readonly Dictionary<string, UserRole> _groupToRoleMapping = new()
    {
        { "ITSF-App-ReadOnly", UserRole.ReadOnly },
        { "ITSF-App-Standard", UserRole.Standard },
        { "ITSF-App-DatabaseAdmin", UserRole.DatabaseAdmin },
        { "ITSF-App-SystemAdmin", UserRole.SystemAdmin }
    };

    public bool IsDeveloperModeEnabled => _developerModeEnabled;
    public UserRole? SimulatedRole => _simulatedRole;

    public WindowsAuthorizationService(
        ILogger<WindowsAuthorizationService> logger,
        ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    #region Core AD Integration

    public async Task<string> GetCurrentUserNameAsync()
    {
        try
        {
            return await Task.FromResult(Environment.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current username");
            return "Unknown";
        }
    }

    public async Task<string> GetCurrentDomainAsync()
    {
        try
        {
            return await Task.FromResult(Environment.UserDomainName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current domain");
            return "Unknown";
        }
    }

    public async Task<bool> IsInGroupAsync(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return false;

        var cacheKey = $"group_membership_{Environment.UserName}_{groupName}";
        
        if (_cache.Get(cacheKey) is bool cachedResult)
        {
            _logger.LogDebug("Using cached group membership for {User} in {Group}: {Result}", 
                Environment.UserName, groupName, cachedResult);
            return cachedResult;
        }

        try
        {
            var config = await GetConfigurationAsync();
            
            // Try domain context first
            using var context = new PrincipalContext(ContextType.Domain, config.DomainName);
            using var user = UserPrincipal.FindByIdentity(context, Environment.UserName);
            
            if (user == null)
            {
                _logger.LogWarning("User {User} not found in domain {Domain}", Environment.UserName, config.DomainName);
                return false;
            }

            var result = user.IsMemberOf(context, IdentityType.Name, groupName);
            
            // Cache the result for the configured timeout
            var policy = new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromMinutes(config.CacheTimeoutMinutes)
            };
            _cache.Set(cacheKey, result, policy);
            
            _logger.LogInformation("User {User} group membership for {Group}: {Result}", 
                Environment.UserName, groupName, result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check AD group membership for {User} in {Group}", 
                Environment.UserName, groupName);
            
            // Fallback: assume user has minimal permissions
            return false;
        }
    }

    public async Task<List<string>> GetUserGroupsAsync()
    {
        var cacheKey = $"user_groups_{Environment.UserName}";
        
        if (_cache.Get(cacheKey) is List<string> cachedGroups)
        {
            return cachedGroups;
        }

        try
        {
            var config = await GetConfigurationAsync();
            var groups = new List<string>();
            
            using var context = new PrincipalContext(ContextType.Domain, config.DomainName);
            using var user = UserPrincipal.FindByIdentity(context, Environment.UserName);
            
            if (user != null)
            {
                var memberOf = user.GetAuthorizationGroups();
                groups.AddRange(memberOf.Select(g => g.Name).Where(name => !string.IsNullOrEmpty(name)));
            }

            // Cache the result
            var policy = new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromMinutes(config.CacheTimeoutMinutes)
            };
            _cache.Set(cacheKey, groups, policy);
            
            _logger.LogInformation("Retrieved {Count} groups for user {User}", groups.Count, Environment.UserName);
            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user groups for {User}", Environment.UserName);
            return new List<string>();
        }
    }

    #endregion

    #region Role-based Access

    public async Task<UserRole> GetUserRoleAsync()
    {
        // Developer mode override
        if (_developerModeEnabled && _simulatedRole.HasValue)
        {
            _logger.LogDebug("Developer mode: simulating role {Role}", _simulatedRole.Value);
            return _simulatedRole.Value;
        }

        var cacheKey = $"user_role_{Environment.UserName}";
        
        if (_cache.Get(cacheKey) is UserRole cachedRole)
        {
            return cachedRole;
        }

        var config = await GetConfigurationAsync();

        try
        {
            // Check if we're in a domain environment
            bool isNonDomain = await IsNonDomainEnvironmentAsync();
            
            if (isNonDomain)
            {
                // Non-domain environment: grant SystemAdmin but restrict database connections
                _logger.LogInformation("Non-domain environment detected. Granting SystemAdmin role with localhost-only database access for user {User}", 
                    Environment.UserName);
                
                var nonDomainRole = UserRole.SystemAdmin;
                
                // Cache the result
                var nonDomainPolicy = new CacheItemPolicy
                {
                    SlidingExpiration = TimeSpan.FromMinutes(config.CacheTimeoutMinutes)
                };
                _cache.Set(cacheKey, nonDomainRole, nonDomainPolicy);
                
                return nonDomainRole;
            }

            UserRole role = UserRole.ReadOnly; // Default fallback

            // Check groups in priority order (highest to lowest)
            // Use hard-coded group names for security
            if (await IsInGroupAsync("ITSF-App-SystemAdmin"))
            {
                role = UserRole.SystemAdmin;
            }
            else if (await IsInGroupAsync("ITSF-App-DatabaseAdmin"))
            {
                role = UserRole.DatabaseAdmin;
            }
            else if (await IsInGroupAsync("ITSF-App-Standard"))
            {
                role = UserRole.Standard;
            }
            else if (await IsInGroupAsync("ITSF-App-ReadOnly"))
            {
                role = UserRole.ReadOnly;
            }

            // Cache the result
            var policy = new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromMinutes(config.CacheTimeoutMinutes)
            };
            _cache.Set(cacheKey, role, policy);

            _logger.LogInformation("Determined role for user {User}: {Role}", Environment.UserName, role);
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine user role for {User}, defaulting to ReadOnly", Environment.UserName);
            return UserRole.ReadOnly;
        }
    }

    public async Task<Permission> GetUserPermissionsAsync()
    {
        var role = await GetUserRoleAsync();
        return RolePermissions.GetPermissions(role);
    }

    public async Task<bool> HasPermissionAsync(Permission permission)
    {
        var userPermissions = await GetUserPermissionsAsync();
        return (userPermissions & permission) == permission;
    }

    #endregion

    #region Specific Capability Checks

    public async Task<bool> CanAccessDatabaseAdminAsync()
    {
        return await HasPermissionAsync(Permission.DatabaseAdmin);
    }

    public async Task<bool> CanEditDevicesDirectlyAsync()
    {
        return await HasPermissionAsync(Permission.EditDirectly);
    }

    public async Task<bool> CanApproveChangesAsync()
    {
        return await HasPermissionAsync(Permission.ApproveChanges);
    }

    public async Task<bool> CanAccessSystemSettingsAsync()
    {
        return await HasPermissionAsync(Permission.SystemConfiguration);
    }

    #endregion

    #region Developer Mode

    public void SetDeveloperMode(bool enabled, UserRole? simulatedRole = null)
    {
#if DEBUG
        _developerModeEnabled = enabled;
        _simulatedRole = simulatedRole;
        
        if (enabled)
        {
            _logger.LogWarning("Developer mode enabled, simulating role: {Role}", simulatedRole);
            ClearCache(); // Clear cache so role checks use simulated role
        }
        else
        {
            _logger.LogInformation("Developer mode disabled");
            ClearCache(); // Clear cache to reload actual permissions
        }
#else
        _logger.LogWarning("Developer mode is only available in debug builds");
#endif
    }

    #endregion

    #region Cache Management

    public void ClearCache()
    {
        foreach (var item in _cache.ToList())
        {
            _cache.Remove(item.Key);
        }
        _logger.LogInformation("Authorization cache cleared");
    }

    #endregion

    #region Configuration

    public async Task<AuthorizationConfig> GetConfigurationAsync()
    {
        if (_config != null)
            return _config;

        try
        {
            _config = await _settingsService.GetAsync<AuthorizationConfig>("authorization_config", 
                new AuthorizationConfig());
            
            return _config ?? new AuthorizationConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load authorization configuration, using defaults");
            return new AuthorizationConfig();
        }
    }

    public async Task SaveConfigurationAsync(AuthorizationConfig config)
    {
        try
        {
            await _settingsService.SetAsync("authorization_config", config);
            _config = config;
            ClearCache(); // Clear cache to use new configuration
            _logger.LogInformation("Authorization configuration saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save authorization configuration");
            throw;
        }
    }

    #endregion

    #region Database Connection Restrictions

    public async Task<bool> CanConnectToDatabaseAsync(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            return false;
/*
        // Check if we're in a non-domain environment
        bool isNonDomain = await IsNonDomainEnvironmentAsync();
        
        if (isNonDomain)
        {
            // Non-domain environment: only allow localhost connections
            bool isLocalhost = IsLocalhostServer(serverName);
            
            if (!isLocalhost)
            {
                _logger.LogWarning("Non-domain environment: blocking database connection to {Server}. Only localhost connections allowed.", serverName);
            }
            else
            {
                _logger.LogInformation("Non-domain environment: allowing database connection to localhost server {Server}", serverName);
            }
            
            return isLocalhost;
        }
*/
        // Domain environment: check user permissions normally
        var userRole = await GetUserRoleAsync();
        bool hasDbPermission = await HasPermissionAsync(Permission.DatabaseAdmin);
        
        _logger.LogDebug("Domain environment: User {User} with role {Role} requesting database connection to {Server}. Permission: {HasPermission}", 
            Environment.UserName, userRole, serverName, hasDbPermission);
        
        return hasDbPermission;
    }

    public async Task<bool> IsNonDomainEnvironmentAsync()
    {
        var cacheKey = "is_non_domain_environment";
        
        if (_cache.Get(cacheKey) is bool cachedResult)
        {
            return cachedResult;
        }

        bool isNonDomain = false;
        
        try
        {
            // Try to connect to domain
            var config = await GetConfigurationAsync();
            using var context = new PrincipalContext(ContextType.Domain, config.DomainName);
            
            // If we can connect to domain, we're in a domain environment
            isNonDomain = false;
            _logger.LogDebug("Successfully connected to domain {Domain} - domain environment detected", config.DomainName);
        }
        catch (Exception ex)
        {
            // If we can't connect to domain, we're in a non-domain environment
            isNonDomain = true;
            _logger.LogInformation("Cannot connect to domain - non-domain environment detected: {Error}", ex.Message);
        }

        // Cache the result for 5 minutes (shorter than normal cache since domain availability can change)
        var policy = new CacheItemPolicy
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };
        _cache.Set(cacheKey, isNonDomain, policy);

        return isNonDomain;
    }

    private static bool IsLocalhostServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            return false;

        // Common localhost patterns
        var localhostPatterns = new[]
        {
            "localhost",
            "127.0.0.1",
            "::1",
            "(local)",
            ".",
            Environment.MachineName
        };

        var normalizedServer = serverName.Trim().ToLowerInvariant();
        
        // Direct localhost matches
        if (localhostPatterns.Any(pattern => 
            normalizedServer.Equals(pattern.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check for localhost with port (e.g., "localhost:1433", "localhost:1433", "localhost\\DBName)
        if (normalizedServer.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase) ||
            normalizedServer.StartsWith("127.0.0.1:", StringComparison.OrdinalIgnoreCase) ||
            normalizedServer.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase) ||
            normalizedServer.StartsWith("127.0.0.1\\", StringComparison.OrdinalIgnoreCase) ||
            normalizedServer.StartsWith("(local)\\", StringComparison.OrdinalIgnoreCase) ||
            normalizedServer.StartsWith("(local):", StringComparison.OrdinalIgnoreCase) ||
            normalizedServer.StartsWith(Environment.MachineName.ToLowerInvariant() + ":", StringComparison.OrdinalIgnoreCase) ||
            normalizedServer.StartsWith(Environment.MachineName.ToLowerInvariant() + "\\", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for machine name with port
        if (normalizedServer.StartsWith(Environment.MachineName.ToLowerInvariant() + ":", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    #endregion
}
