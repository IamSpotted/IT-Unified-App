namespace MauiApp1.Interfaces;

/// <summary>
/// Interface for admin privilege detection service
/// </summary>
public interface IAdminService : ITransientService
{
    /// <summary>
    /// Gets whether the current process is running with administrator privileges
    /// </summary>
    bool IsRunningAsAdmin { get; }

    /// <summary>
    /// Gets the admin status display text
    /// </summary>
    string AdminStatusText { get; }

    /// <summary>
    /// Gets the admin status icon
    /// </summary>
    string AdminStatusIcon { get; }
}
