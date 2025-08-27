using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Extensions.Logging;
using System.ServiceProcess;

namespace MauiApp1.Scripts.System;

/// <summary>
/// Windows Services management and monitoring script
/// </summary>
public class WindowsServicesScript : BaseAutomationScript
{
    public WindowsServicesScript(ILogger<WindowsServicesScript> logger) : base(logger)
    {
    }

    public override string Id => "windows-services";
    public override string Name => "Windows Services Monitor";
    public override string Description => "Lists and monitors Windows services status, with options to start/stop services";
    public override string Category => "System";
    public override bool RequiresAdmin => true; // Required for service management
    public override string EstimatedDuration => "15 seconds";
    public override string Author => "ITSF Admin";
    public override string Version => "1.0.0";

    public override async Task<ScriptValidationResult> ValidateAsync()
    {
        var result = new ScriptValidationResult { IsValid = true };

        // Check admin privileges for service management
        if (!IsRunningAsAdministrator())
        {
            result.Warnings.Add("Not running as administrator - service start/stop operations will not be available");
        }

        // Check if sc command is available for service control
        if (!await IsCommandAvailableAsync("sc"))
        {
            result.Warnings.Add("sc command not available - service control functionality may be limited");
        }

        return result;
    }

    protected override async Task<ScriptExecutionResult> ExecuteInternalAsync(Dictionary<string, object>? parameters, CancellationToken cancellationToken)
    {
        var result = new ScriptExecutionResult();
        var output = new List<string>();

        try
        {
            output.Add("=== WINDOWS SERVICES MONITOR ===");
            output.Add($"Scan started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            output.Add($"Administrator privileges: {(IsRunningAsAdministrator() ? "✓ Available" : "✗ Not available")}");
            output.Add("");

            // Get service action from parameters
            var action = parameters?.GetValueOrDefault("action")?.ToString()?.ToLower();
            var serviceName = parameters?.GetValueOrDefault("serviceName")?.ToString();

            if (!string.IsNullOrEmpty(action) && !string.IsNullOrEmpty(serviceName))
            {
                await PerformServiceAction(output, action, serviceName);
            }
            else
            {
                await ListAllServices(output);
            }

            result.Success = true;
            result.Output = string.Join(Environment.NewLine, output);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Windows services operation failed: {ex.Message}";
            Logger.LogError(ex, "Windows services script failed");
        }

        return result;
    }

    private async Task ListAllServices(List<string> output)
    {
        try
        {
            // Get all services using ServiceController
            var services = ServiceController.GetServices();
            
            // Categorize services
            var runningServices = new List<ServiceController>();
            var stoppedServices = new List<ServiceController>();
            var otherServices = new List<ServiceController>();

            foreach (var service in services)
            {
                try
                {
                    switch (service.Status)
                    {
                        case ServiceControllerStatus.Running:
                            runningServices.Add(service);
                            break;
                        case ServiceControllerStatus.Stopped:
                            stoppedServices.Add(service);
                            break;
                        default:
                            otherServices.Add(service);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to get status for service: {ServiceName}", service.ServiceName);
                }
            }

            // Summary
            output.Add("=== SERVICE SUMMARY ===");
            output.Add($"Total Services: {services.Length}");
            output.Add($"Running: {runningServices.Count}");
            output.Add($"Stopped: {stoppedServices.Count}");
            output.Add($"Other States: {otherServices.Count}");
            output.Add("");

            // Critical system services check
            CheckCriticalServices(output);

            // Running services
            output.Add("=== RUNNING SERVICES ===");
            foreach (var service in runningServices.OrderBy(s => s.DisplayName))
            {
                try
                {
                    output.Add($"✓ {service.DisplayName} ({service.ServiceName})");
                    
                    // Try to get additional info
                    var startType = await GetServiceStartType(service.ServiceName);
                    if (!string.IsNullOrEmpty(startType))
                    {
                        output.Add($"    Start Type: {startType}");
                    }
                }
                catch (Exception ex)
                {
                    output.Add($"✓ {service.ServiceName} (Error getting details: {ex.Message})");
                }
            }
            output.Add("");

            // Stopped critical services
            var stoppedCritical = stoppedServices.Where(s => IsCriticalService(s.ServiceName)).ToList();
            if (stoppedCritical.Any())
            {
                output.Add("=== STOPPED CRITICAL SERVICES ===");
                foreach (var service in stoppedCritical)
                {
                    output.Add($"⚠️  {service.DisplayName} ({service.ServiceName})");
                }
                output.Add("");
            }

            // Services in other states
            if (otherServices.Any())
            {
                output.Add("=== SERVICES IN TRANSITION ===");
                foreach (var service in otherServices.OrderBy(s => s.DisplayName))
                {
                    try
                    {
                        var statusIcon = GetStatusIcon(service.Status);
                        output.Add($"{statusIcon} {service.DisplayName} ({service.ServiceName}) - {service.Status}");
                    }
                    catch (Exception ex)
                    {
                        output.Add($"? {service.ServiceName} (Error: {ex.Message})");
                    }
                }
                output.Add("");
            }

            // Dispose services
            foreach (var service in services)
            {
                try
                {
                    service.Dispose();
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            output.Add($"Error listing services: {ex.Message}");
            Logger.LogError(ex, "Failed to list Windows services");
        }
    }

    private void CheckCriticalServices(List<string> output)
    {
        var criticalServices = new[]
        {
            "BITS", "EventLog", "RpcSs", "LanmanServer", "LanmanWorkstation",
            "Dhcp", "Dnscache", "Themes", "AudioSrv", "Spooler"
        };

        output.Add("=== CRITICAL SERVICES STATUS ===");
        
        foreach (var serviceName in criticalServices)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                var status = service.Status;
                var icon = status == ServiceControllerStatus.Running ? "✓" : "⚠️";
                output.Add($"{icon} {service.DisplayName}: {status}");
            }
            catch (Exception)
            {
                output.Add($"? {serviceName}: Not found or inaccessible");
            }
        }
        
        output.Add("");
    }

    private async Task PerformServiceAction(List<string> output, string action, string serviceName)
    {
        if (!IsRunningAsAdministrator())
        {
            output.Add($"❌ Cannot perform service action '{action}' - Administrator privileges required");
            return;
        }

        try
        {
            using var service = new ServiceController(serviceName);
            output.Add($"=== SERVICE ACTION: {action.ToUpper()} ===");
            output.Add($"Service: {service.DisplayName} ({serviceName})");
            output.Add($"Current Status: {service.Status}");
            output.Add("");

            switch (action)
            {
                case "start":
                    if (service.Status == ServiceControllerStatus.Stopped)
                    {
                        output.Add("Starting service...");
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                        output.Add($"✓ Service started successfully. Status: {service.Status}");
                    }
                    else
                    {
                        output.Add($"⚠️  Service is already {service.Status}");
                    }
                    break;

                case "stop":
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        output.Add("Stopping service...");
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        output.Add($"✓ Service stopped successfully. Status: {service.Status}");
                    }
                    else
                    {
                        output.Add($"⚠️  Service is already {service.Status}");
                    }
                    break;

                case "restart":
                    output.Add("Restarting service...");
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    output.Add($"✓ Service restarted successfully. Status: {service.Status}");
                    break;

                case "status":
                    service.Refresh();
                    output.Add($"Service Status: {service.Status}");
                    
                    var startType = await GetServiceStartType(serviceName);
                    if (!string.IsNullOrEmpty(startType))
                    {
                        output.Add($"Start Type: {startType}");
                    }
                    break;

                default:
                    output.Add($"❌ Unknown action: {action}");
                    output.Add("Available actions: start, stop, restart, status");
                    break;
            }
        }
        catch (Exception ex)
        {
            output.Add($"❌ Service action failed: {ex.Message}");
            Logger.LogError(ex, "Failed to perform service action {Action} on {ServiceName}", action, serviceName);
        }
    }

    private async Task<string> GetServiceStartType(string serviceName)
    {
        try
        {
            var result = await ExecuteCommandAsync($"sc qc \"{serviceName}\"");
            if (result.Success)
            {
                var lines = result.Output.Split('\n');
                var startTypeLine = lines.FirstOrDefault(l => l.Trim().StartsWith("START_TYPE"));
                if (startTypeLine != null)
                {
                    var parts = startTypeLine.Split(':');
                    if (parts.Length > 1)
                    {
                        return parts[1].Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get start type for service {ServiceName}", serviceName);
        }

        return string.Empty;
    }

    private bool IsCriticalService(string serviceName)
    {
        var criticalServices = new[]
        {
            "BITS", "EventLog", "RpcSs", "LanmanServer", "LanmanWorkstation",
            "Dhcp", "Dnscache", "Themes", "AudioSrv", "Spooler", "W32Time",
            "WinRM", "TermService", "Schedule"
        };

        return criticalServices.Contains(serviceName, StringComparer.OrdinalIgnoreCase);
    }

    private string GetStatusIcon(ServiceControllerStatus status)
    {
        return status switch
        {
            ServiceControllerStatus.Running => "✓",
            ServiceControllerStatus.Stopped => "⚪",
            ServiceControllerStatus.StartPending => "🔄",
            ServiceControllerStatus.StopPending => "⏸️",
            ServiceControllerStatus.Paused => "⏸️",
            ServiceControllerStatus.PausePending => "⏸️",
            ServiceControllerStatus.ContinuePending => "🔄",
            _ => "?"
        };
    }

    private async Task<bool> IsCommandAvailableAsync(string command)
    {
        try
        {
            var result = await ExecuteCommandAsync($"where {command}");
            return result.Success;
        }
        catch
        {
            return false;
        }
    }
}
