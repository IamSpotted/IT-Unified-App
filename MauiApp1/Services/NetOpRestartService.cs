using System.Management;
using System.ServiceProcess;
using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.Services;

/// <summary>
/// Service implementation for NetOp service restart operations.
/// Handles restarting NetOp/Impero services on local and remote computers.
/// Based on proven RestartImpero console implementation.
/// </summary>
public class NetOpRestartService : INetOpRestartService, ITransientService
{
    private const string ImperoServiceNamePattern = "NetOp";
    private static readonly string[] ProcessNamePatterns = { "nh*", "impero*" };

    /// <inheritdoc />
    public async Task<NetOpRestartResult> RestartNetOpServiceAsync(string computerName)
    {
        if (string.IsNullOrWhiteSpace(computerName))
        {
            return new NetOpRestartResult
            {
                ComputerName = computerName ?? "Unknown",
                Status = "Failed",
                Message = "Computer name cannot be empty",
                IsCompleted = true,
                Timestamp = DateTime.Now
            };
        }

        try
        {
            // If local computer, use ServiceController for services, but still terminate processes
            if (IsLocalComputer(computerName))
            {
                return await RestartLocalNetOpServiceAsync(computerName);
            }
            else
            {
                return await RestartRemoteNetOpServiceAsync(computerName);
            }
        }
        catch (Exception ex)
        {
            return new NetOpRestartResult
            {
                ComputerName = computerName,
                Status = "Failed",
                Message = $"Unexpected error: {ex.Message}",
                IsCompleted = true,
                Timestamp = DateTime.Now
            };
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NetOpRestartResult>> RestartNetOpServicesAsync(IEnumerable<string> computerNames)
    {
        var tasks = computerNames.Select(RestartNetOpServiceAsync);
        return await Task.WhenAll(tasks);
    }

    private bool IsLocalComputer(string computerName)
    {
        var localNames = new[] { "localhost", "127.0.0.1", ".", Environment.MachineName };
        return localNames.Any(name => string.Equals(name, computerName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<NetOpRestartResult> RestartLocalNetOpServiceAsync(string computerName)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Step 1: Terminate Impero processes first
                TerminateImperoProcesses(computerName);
                
                // Step 2: Wait for processes to fully terminate
                Thread.Sleep(2000);
                
                // Step 3: Restart the service
                var services = GetServicesByPattern(computerName, ImperoServiceNamePattern);
                
                if (services.Any())
                {
                    foreach (var serviceName in services)
                    {
                        if (string.IsNullOrEmpty(serviceName))
                            continue;

                        using var serviceController = new ServiceController(serviceName, computerName);
                        
                        if (serviceController.Status != ServiceControllerStatus.Stopped)
                        {
                            serviceController.Stop();
                            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        }

                        serviceController.Start();
                        serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                        if (serviceController.Status == ServiceControllerStatus.Running)
                        {
                            return new NetOpRestartResult
                            {
                                ComputerName = computerName,
                                Status = "Success",
                                Message = $"Successfully restarted {serviceName} service",
                                IsCompleted = true,
                                Timestamp = DateTime.Now
                            };
                        }
                    }
                }

                return new NetOpRestartResult
                {
                    ComputerName = computerName,
                    Status = "Failed",
                    Message = "No NetOp services found on this computer",
                    IsCompleted = true,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new NetOpRestartResult
                {
                    ComputerName = computerName,
                    Status = "Failed",
                    Message = $"Error restarting service: {ex.Message}",
                    IsCompleted = true,
                    Timestamp = DateTime.Now
                };
            }
        });
    }

    private async Task<NetOpRestartResult> RestartRemoteNetOpServiceAsync(string computerName)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Step 1: Terminate Impero processes first  
                TerminateImperoProcesses(computerName);
                
                // Step 2: Wait for processes to fully terminate
                Thread.Sleep(2000);
                
                // Step 3: Restart the service
                var services = GetServicesByPattern(computerName, ImperoServiceNamePattern);

                if (services.Any())
                {
                    foreach (var serviceName in services)
                    {
                        if (string.IsNullOrEmpty(serviceName))
                            continue;

                        using var serviceController = new ServiceController(serviceName, computerName);
                        
                        if (serviceController.Status != ServiceControllerStatus.Stopped)
                        {
                            serviceController.Stop();
                            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        }

                        serviceController.Start();
                        serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                        if (serviceController.Status == ServiceControllerStatus.Running)
                        {
                            return new NetOpRestartResult
                            {
                                ComputerName = computerName,
                                Status = "Success",
                                Message = $"Successfully restarted {serviceName} service",
                                IsCompleted = true,
                                Timestamp = DateTime.Now
                            };
                        }
                    }
                }

                return new NetOpRestartResult
                {
                    ComputerName = computerName,
                    Status = "Failed",
                    Message = "No NetOp services found or accessible on this computer",
                    IsCompleted = true,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new NetOpRestartResult
                {
                    ComputerName = computerName,
                    Status = "Failed",
                    Message = $"Remote access failed: {ex.Message}",
                    IsCompleted = true,
                    Timestamp = DateTime.Now
                };
            }
        });
    }

    /// <summary>
    /// Terminates Impero processes on the specified computer using WMI.
    /// Based on proven RestartImpero implementation.
    /// </summary>
    private void TerminateImperoProcesses(string computerName)
    {
        try
        {
            var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
            
            foreach (var pattern in ProcessNamePatterns)
            {
                var query = new ObjectQuery($"SELECT * FROM Win32_Process WHERE Name LIKE '{pattern}' COLLATE Latin1_General_CI_AS");
                using var searcher = new ManagementObjectSearcher(scope, query);
                
                foreach (ManagementObject process in searcher.Get())
                {
                    var processId = process["Handle"]?.ToString();
                    var processName = process["Name"]?.ToString();
                    var returnValue = process.InvokeMethod("Terminate", null);
                    
                    // Note: In GUI app, we don't log to console like the original
                    // The calling method will handle success/failure reporting
                }
            }
        }
        catch
        {
            // Silently continue - the calling method will handle overall error reporting
        }
    }

    /// <summary>
    /// Gets services matching the specified pattern on the target computer.
    /// Based on proven RestartImpero implementation.
    /// </summary>
    private IEnumerable<string> GetServicesByPattern(string computerName, string pattern)
    {
        try
        {
            var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
            var query = new ObjectQuery($"SELECT * FROM Win32_Service WHERE Name LIKE '{pattern}%'");
            using var searcher = new ManagementObjectSearcher(scope, query);
            
            var services = searcher.Get()
                                .Cast<ManagementObject>()
                                .Select(service => service["Name"]?.ToString())
                                .Where(name => !string.IsNullOrEmpty(name))
                                .Cast<string>()
                                .ToList();
            return services;
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }
}
