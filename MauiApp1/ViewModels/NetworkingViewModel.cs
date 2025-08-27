using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MauiApp1.Views;
using MauiApp1.Scripts.Network;

namespace MauiApp1.ViewModels;

public partial class NetworkingViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _message = "Welcome to Networking Tools";

    public NetworkingViewModel(
        ILogger<NetworkingViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService) 
        : base(logger, navigationService, dialogService)
    {
        Title = "Networking";
    }

    [RelayCommand]
    private async Task LaunchConnectivityTest()
    {
        try
        {
            _logger.LogInformation("Launching Connectivity Test from Networking page");
            await _navigationService.NavigateToAsync(nameof(ConnectivityTestPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Connectivity Test");
            await _dialogService.ShowAlertAsync("Error", $"Failed to open Connectivity Test: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LaunchMultiPing()
    {
        try
        {
            _logger.LogInformation("Launching Multi-Ping Dashboard from Networking page");
            await _navigationService.NavigateToAsync(nameof(MultiPingDashboardPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Multi-Ping Dashboard");
            await _dialogService.ShowAlertAsync("Error", $"Failed to open Multi-Ping Dashboard: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RunNetworkDiagnostics()
    {
        try
        {
            _logger.LogInformation("Running Network Diagnostics from Networking page");
            
            // Show loading message
            var loadingTask = _dialogService.ShowLoadingAsync("Running network diagnostics...");
            
            try
            {
                var diagnosticsScript = new NetworkDiagnosticsScript(_logger as ILogger<NetworkDiagnosticsScript> ?? 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NetworkDiagnosticsScript>.Instance);
                
                var validationResult = await diagnosticsScript.ValidateAsync();
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("\n", validationResult.Errors);
                    await _dialogService.ShowAlertAsync("Validation Failed", $"Cannot run diagnostics:\n{errors}");
                    return;
                }

                var result = await diagnosticsScript.ExecuteAsync();
                
                if (result.Success)
                {
                    await _dialogService.ShowAlertAsync("Network Diagnostics Complete", 
                        result.Output, "OK");
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Diagnostics Failed", 
                        result.Error ?? "Unknown error occurred", "OK");
                }
            }
            finally
            {
                // Complete any loading dialog if still showing
                await loadingTask;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run network diagnostics");
            await _dialogService.ShowAlertAsync("Error", $"Failed to run network diagnostics: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ShowIpConfig()
    {
        try
        {
            await _dialogService.ShowAlertAsync("IP Configuration", 
                "Opening IP configuration details...\n\nThis will show your current network adapter settings including IP addresses, subnet masks, and DNS servers.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show IP config");
            await _dialogService.ShowAlertAsync("Error", $"Failed to show IP configuration: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ShowDnsLookup()
    {
        try
        {
            _logger.LogInformation("Launching DNS Lookup tool from Networking page");
            await _navigationService.NavigateToAsync(nameof(DnsLookupPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to DNS Lookup");
            await _dialogService.ShowAlertAsync("Error", $"Failed to open DNS Lookup tool: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ShowNetStats()
    {
        try
        {
            await _dialogService.ShowAlertAsync("Network Statistics", 
                "Opening network statistics...\n\nThis will display detailed network interface statistics including bytes sent/received, packets, and errors.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show network stats");
            await _dialogService.ShowAlertAsync("Error", $"Failed to show network statistics: {ex.Message}");
        }
    }
}
