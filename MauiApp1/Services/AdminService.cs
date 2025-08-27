using MauiApp1.Interfaces;

namespace MauiApp1.Services;

/// <summary>
/// Admin privilege detection service implementation
/// </summary>
public class AdminService : IAdminService
{
    private readonly ILogger<AdminService> _logger;
    private readonly bool _isRunningAsAdmin;

    public bool IsRunningAsAdmin => _isRunningAsAdmin;

    public string AdminStatusText => _isRunningAsAdmin ? "ADMIN" : "USER";

    public string AdminStatusIcon => _isRunningAsAdmin ? "●" : "○";

    public AdminService(ILogger<AdminService> logger)
    {
        _logger = logger;
        _isRunningAsAdmin = CheckIfRunningAsAdmin();
        
        _logger.LogInformation("Application running as: {AdminStatus}", AdminStatusText);
    }

    private bool CheckIfRunningAsAdmin()
    {
        try
        {
#if WINDOWS
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
#else
            // For non-Windows platforms, check if running as root
            return Environment.UserName.Equals("root", StringComparison.OrdinalIgnoreCase) ||
                   System.Environment.GetEnvironmentVariable("USER")?.Equals("root", StringComparison.OrdinalIgnoreCase) == true;
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check admin status");
            return false;
        }
    }
}
