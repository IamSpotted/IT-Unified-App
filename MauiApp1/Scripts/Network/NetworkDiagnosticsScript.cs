using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Extensions.Logging;

namespace MauiApp1.Scripts.Network;

/// <summary>
/// Network diagnostics and connectivity testing script
/// </summary>
public class NetworkDiagnosticsScript : BaseAutomationScript
{
    public NetworkDiagnosticsScript(ILogger<NetworkDiagnosticsScript> logger) : base(logger)
    {
    }

    public override string Id => "network-diagnostics";
    public override string Name => "Network Diagnostics";
    public override string Description => "Performs comprehensive network connectivity and configuration testing";
    public override string Category => "Network";
    public override bool RequiresAdmin => false;
    public override string EstimatedDuration => "30 seconds";
    public override string Author => "ITSF Admin";
    public override string Version => "1.0.0";

    public override async Task<ScriptValidationResult> ValidateAsync()
    {
        var result = new ScriptValidationResult { IsValid = true };

        // Check if required commands are available
        if (!await IsCommandAvailableAsync("ping"))
        {
            result.Errors.Add("ping command not available");
            result.IsValid = false;
        }

        if (!await IsCommandAvailableAsync("ipconfig"))
        {
            result.Warnings.Add("ipconfig command not available - IP configuration details will be limited");
        }

        if (!await IsCommandAvailableAsync("nslookup"))
        {
            result.Warnings.Add("nslookup command not available - DNS resolution testing will be skipped");
        }

        return result;
    }

    protected override async Task<ScriptExecutionResult> ExecuteInternalAsync(Dictionary<string, object>? parameters, CancellationToken cancellationToken)
    {
        var result = new ScriptExecutionResult();
        var output = new List<string>();

        try
        {
            output.Add("=== NETWORK DIAGNOSTICS ===");
            output.Add($"Test started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            output.Add("");

            // IP Configuration
            await GetIpConfiguration(output);

            // Connectivity Tests
            await TestConnectivity(output);

            // DNS Resolution Tests
            await TestDnsResolution(output);

            // Network Interface Statistics
            await GetNetworkStatistics(output);

            result.Success = true;
            result.Output = string.Join(Environment.NewLine, output);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Network diagnostics failed: {ex.Message}";
            Logger.LogError(ex, "Network diagnostics script failed");
        }

        return result;
    }

    private async Task GetIpConfiguration(List<string> output)
    {
        try
        {
            output.Add("=== IP CONFIGURATION ===");

            var result = await ExecuteCommandAsync("ipconfig /all");
            if (result.Success)
            {
                // Parse and format the output
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var relevantLines = lines.Where(line => 
                    line.Contains("Ethernet adapter") ||
                    line.Contains("Wireless LAN adapter") ||
                    line.Contains("IPv4 Address") ||
                    line.Contains("Subnet Mask") ||
                    line.Contains("Default Gateway") ||
                    line.Contains("DNS Servers") ||
                    line.Contains("DHCP Enabled") ||
                    line.Contains("Physical Address")
                ).ToList();

                foreach (var line in relevantLines)
                {
                    output.Add(line.Trim());
                }
            }
            else
            {
                output.Add("Failed to get IP configuration");
            }

            output.Add("");
        }
        catch (Exception ex)
        {
            output.Add($"Error getting IP configuration: {ex.Message}");
            output.Add("");
            Logger.LogWarning(ex, "Failed to get IP configuration");
        }
    }

    private async Task TestConnectivity(List<string> output)
    {
        output.Add("=== CONNECTIVITY TESTS ===");

        var testTargets = new[]
        {
            ("Google DNS", "8.8.8.8"),
            ("Cloudflare DNS", "1.1.1.1"),
            ("Microsoft", "microsoft.com"),
            ("Google", "google.com")
        };

        foreach (var (name, target) in testTargets)
        {
            try
            {
                var result = await ExecuteCommandAsync($"ping -n 4 {target}");
                if (result.Success)
                {
                    // Extract ping statistics
                    var lines = result.Output.Split('\n');
                    var statsLine = lines.FirstOrDefault(l => l.Contains("Average"));
                    var lossLine = lines.FirstOrDefault(l => l.Contains("Lost"));

                    if (statsLine != null && lossLine != null)
                    {
                        output.Add($"{name} ({target}): ✓ REACHABLE");
                        output.Add($"  {lossLine.Trim()}");
                        output.Add($"  {statsLine.Trim()}");
                    }
                    else
                    {
                        output.Add($"{name} ({target}): ✓ REACHABLE");
                    }
                }
                else
                {
                    output.Add($"{name} ({target}): ✗ UNREACHABLE");
                }
            }
            catch (Exception ex)
            {
                output.Add($"{name} ({target}): ✗ ERROR - {ex.Message}");
                Logger.LogWarning(ex, "Failed to ping {Target}", target);
            }

            // Small delay between pings
            await Task.Delay(500);
        }

        output.Add("");
    }

    private async Task TestDnsResolution(List<string> output)
    {
        output.Add("=== DNS RESOLUTION TESTS ===");

        var testDomains = new[]
        {
            "google.com",
            "microsoft.com",
            "github.com",
            "stackoverflow.com"
        };

        foreach (var domain in testDomains)
        {
            try
            {
                var result = await ExecuteCommandAsync($"nslookup {domain}");
                if (result.Success && !result.Output.Contains("can't find"))
                {
                    // Extract IP addresses from nslookup output
                    var lines = result.Output.Split('\n');
                    var addressLines = lines.Where(l => l.Contains("Address:") && !l.Contains("#")).ToList();

                    if (addressLines.Any())
                    {
                        output.Add($"{domain}: ✓ RESOLVED");
                        foreach (var addressLine in addressLines.Take(3)) // Show first 3 addresses
                        {
                            output.Add($"  {addressLine.Trim()}");
                        }
                    }
                    else
                    {
                        output.Add($"{domain}: ✓ RESOLVED (details in nslookup output)");
                    }
                }
                else
                {
                    output.Add($"{domain}: ✗ RESOLUTION FAILED");
                }
            }
            catch (Exception ex)
            {
                output.Add($"{domain}: ✗ ERROR - {ex.Message}");
                Logger.LogWarning(ex, "Failed to resolve {Domain}", domain);
            }
        }

        output.Add("");
    }

    private async Task GetNetworkStatistics(List<string> output)
    {
        try
        {
            output.Add("=== NETWORK STATISTICS ===");

            var result = await ExecuteCommandAsync("netstat -e");
            if (result.Success)
            {
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                // Find the statistics section
                var statsStartIndex = Array.FindIndex(lines, l => l.Contains("Bytes"));
                if (statsStartIndex >= 0 && statsStartIndex + 1 < lines.Length)
                {
                    output.Add("Network Interface Statistics:");
                    output.Add(lines[statsStartIndex].Trim());
                    output.Add(lines[statsStartIndex + 1].Trim());
                }
                else
                {
                    output.Add("Network statistics available in netstat output");
                }
            }
            else
            {
                output.Add("Failed to get network statistics");
            }

            output.Add("");
        }
        catch (Exception ex)
        {
            output.Add($"Error getting network statistics: {ex.Message}");
            output.Add("");
            Logger.LogWarning(ex, "Failed to get network statistics");
        }
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
