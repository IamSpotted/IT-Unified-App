using MauiApp1.Models;

namespace MauiApp1.Interfaces;

/// <summary>
/// Service interface for NetOp service restart operations.
/// Provides methods to restart NetOp/Impero services on target computers.
/// </summary>
public interface INetOpRestartService : ITransientService
{
    /// <summary>
    /// Restarts the NetOp service on a single target computer.
    /// </summary>
    /// <param name="computerName">Computer name or IP address</param>
    /// <returns>Result of the restart operation</returns>
    Task<NetOpRestartResult> RestartNetOpServiceAsync(string computerName);

    /// <summary>
    /// Restarts the NetOp service on multiple target computers.
    /// </summary>
    /// <param name="computerNames">Collection of computer names or IP addresses</param>
    /// <returns>Collection of restart results</returns>
    Task<IEnumerable<NetOpRestartResult>> RestartNetOpServicesAsync(IEnumerable<string> computerNames);
}
