using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MauiApp1.Scripts.System;

/// <summary>
/// System information gathering script
/// </summary>
public class SystemInfoScript : BaseAutomationScript
{
    public SystemInfoScript(ILogger<SystemInfoScript> logger) : base(logger)
    {
    }

    public override string Id => "system-info";
    public override string Name => "System Information";
    public override string Description => "Gathers comprehensive system information including hardware, OS, and performance metrics";
    public override string Category => "System";
    public override bool RequiresAdmin => false;
    public override string EstimatedDuration => "10 seconds";
    public override string Author => "ITSF Admin";
    public override string Version => "1.0.0";

    public override Task<ScriptValidationResult> ValidateAsync()
    {
        var result = new ScriptValidationResult { IsValid = true };

        // Check if required commands are available
        if (!IsCommandAvailable("systeminfo"))
        {
            result.Warnings.Add("systeminfo command not available - some system details may be missing");
        }

        if (!IsCommandAvailable("wmic"))
        {
            result.Warnings.Add("wmic command not available - hardware details may be limited");
        }

        return Task.FromResult(result);
    }

    protected override async Task<ScriptExecutionResult> ExecuteInternalAsync(Dictionary<string, object>? parameters, CancellationToken cancellationToken)
    {
        var result = new ScriptExecutionResult();
        var output = new List<string>();

        try
        {
            // Basic system information
            output.Add("=== SYSTEM INFORMATION ===");
            output.Add($"Computer Name: {Environment.MachineName}");
            output.Add($"User Name: {Environment.UserName}");
            output.Add($"OS Version: {Environment.OSVersion}");
            output.Add($"Processor Count: {Environment.ProcessorCount}");
            output.Add($"Working Set: {Environment.WorkingSet / (1024 * 1024)} MB");
            output.Add("");

            // .NET Information
            output.Add("=== .NET INFORMATION ===");
            output.Add($".NET Version: {Environment.Version}");
            output.Add($"Framework Description: {RuntimeInformation.FrameworkDescription}");
            output.Add($"Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}");
            output.Add("");

            // System uptime
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            output.Add("=== SYSTEM UPTIME ===");
            output.Add($"System Uptime: {uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes");
            output.Add("");

            // Drive information
            output.Add("=== DRIVE INFORMATION ===");
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady)
                    {
                        var freeGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
                        var totalGB = drive.TotalSize / (1024 * 1024 * 1024);
                        var usedGB = totalGB - freeGB;
                        var percentUsed = (double)usedGB / totalGB * 100;

                        output.Add($"Drive {drive.Name}");
                        output.Add($"  Type: {drive.DriveType}");
                        output.Add($"  Format: {drive.DriveFormat}");
                        output.Add($"  Total: {totalGB:N0} GB");
                        output.Add($"  Used: {usedGB:N0} GB ({percentUsed:F1}%)");
                        output.Add($"  Free: {freeGB:N0} GB");
                        output.Add("");
                    }
                }
                catch (Exception ex)
                {
                    output.Add($"Drive {drive.Name}: Error accessing drive - {ex.Message}");
                    output.Add("");
                }
            }

            // Memory information (if wmic is available)
            try
            {
                var memoryInfo = await ExecuteCommandAsync("wmic computersystem get TotalPhysicalMemory /value");
                if (memoryInfo.Success && !string.IsNullOrEmpty(memoryInfo.Output))
                {
                    var lines = memoryInfo.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var memoryLine = lines.FirstOrDefault(l => l.Contains("TotalPhysicalMemory="));
                    if (memoryLine != null)
                    {
                        var memoryValue = memoryLine.Split('=')[1].Trim();
                        if (long.TryParse(memoryValue, out var totalMemory))
                        {
                            var totalGB = totalMemory / (1024 * 1024 * 1024);
                            output.Add("=== MEMORY INFORMATION ===");
                            output.Add($"Total Physical Memory: {totalGB:N0} GB");
                            output.Add("");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get memory information via wmic");
            }

            // Process information
            output.Add("=== TOP PROCESSES BY MEMORY ===");
            var processes = Process.GetProcesses()
                .Where(p => !p.HasExited)
                .OrderByDescending(p => p.WorkingSet64)
                .Take(10)
                .ToList();

            foreach (var process in processes)
            {
                try
                {
                    var memoryMB = process.WorkingSet64 / (1024 * 1024);
                    output.Add($"{process.ProcessName}: {memoryMB:N0} MB");
                }
                catch (Exception ex)
                {
                    output.Add($"{process.ProcessName}: Error reading memory - {ex.Message}");
                }
            }

            // Cleanup processes
            foreach (var process in processes)
            {
                try
                {
                    process.Dispose();
                }
                catch { }
            }

            result.Success = true;
            result.Output = string.Join(Environment.NewLine, output);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Failed to gather system information: {ex.Message}";
            Logger.LogError(ex, "System information script failed");
        }

        return result;
    }

    private bool IsCommandAvailable(string command)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit(1000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
