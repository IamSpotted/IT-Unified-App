using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace MauiApp1.ViewModels;

public partial class DnsLookupViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _queryInput = string.Empty;

    [ObservableProperty]
    private bool _isReverseLookup = false;

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private string _statusMessage = "Ready to perform DNS lookup";

    [ObservableProperty]
    private bool _hasResults = false;

    public ObservableCollection<DnsResult> Results { get; } = new();

    public DnsLookupViewModel(
        ILogger<DnsLookupViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService)
        : base(logger, navigationService, dialogService)
    {
        Title = "DNS Lookup";
    }

    [RelayCommand]
    private async Task PerformLookup()
    {
        if (string.IsNullOrWhiteSpace(QueryInput))
        {
            await _dialogService.ShowAlertAsync("Input Required", "Please enter a hostname or IP address to lookup.");
            return;
        }

        IsRunning = true;
        StatusMessage = "Performing DNS lookup...";
        
        try
        {
            var query = QueryInput.Trim();
            
            if (IsReverseLookup || IsValidIpAddress(query))
            {
                await PerformReverseLookup(query);
            }
            else
            {
                await PerformForwardLookup(query);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNS lookup failed for query: {Query}", QueryInput);
            
            var errorResult = new DnsResult
            {
                QueryType = IsReverseLookup ? "Reverse Lookup" : "Forward Lookup",
                Query = QueryInput.Trim(),
                Status = "Error",
                ErrorMessage = ex.Message,
                Timestamp = DateTime.Now
            };
            
            Results.Add(errorResult);
            StatusMessage = $"DNS lookup failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            HasResults = Results.Any();
        }
    }

    [RelayCommand]
    private void ClearResults()
    {
        Results.Clear();
        HasResults = false;
        StatusMessage = "Ready to perform DNS lookup";
    }

    [RelayCommand]
    private void ToggleLookupType()
    {
        IsReverseLookup = !IsReverseLookup;
        StatusMessage = IsReverseLookup 
            ? "Ready to perform reverse DNS lookup (IP → Hostname)" 
            : "Ready to perform forward DNS lookup (Hostname → IP)";
    }

    private async Task PerformForwardLookup(string hostname)
    {
        StatusMessage = $"Resolving hostname: {hostname}";
        
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(hostname);
            
            var result = new DnsResult
            {
                QueryType = "Forward Lookup",
                Query = hostname,
                HostName = hostEntry.HostName,
                Status = "Success",
                Timestamp = DateTime.Now
            };

            // Add all IP addresses
            foreach (var address in hostEntry.AddressList)
            {
                result.IpAddresses.Add(new IpAddressInfo
                {
                    Address = address.ToString(),
                    AddressFamily = address.AddressFamily.ToString()
                });
            }

            // Add aliases if any
            foreach (var alias in hostEntry.Aliases)
            {
                result.Aliases.Add(alias);
            }

            Results.Add(result);
            StatusMessage = $"Found {hostEntry.AddressList.Length} IP address(es) for {hostname}";
            
            _logger.LogInformation("DNS forward lookup successful for {Hostname}, found {Count} addresses", 
                hostname, hostEntry.AddressList.Length);
        }
        catch (Exception ex)
        {
            var errorResult = new DnsResult
            {
                QueryType = "Forward Lookup",
                Query = hostname,
                Status = "Failed",
                ErrorMessage = ex.Message,
                Timestamp = DateTime.Now
            };
            
            Results.Add(errorResult);
            StatusMessage = $"Forward lookup failed for {hostname}";
            
            _logger.LogWarning(ex, "DNS forward lookup failed for {Hostname}", hostname);
        }
    }

    private async Task PerformReverseLookup(string ipAddress)
    {
        StatusMessage = $"Resolving IP address: {ipAddress}";
        
        try
        {
            if (!IPAddress.TryParse(ipAddress, out var address))
            {
                throw new ArgumentException($"Invalid IP address format: {ipAddress}");
            }

            var hostEntry = await Dns.GetHostEntryAsync(address);
            
            var result = new DnsResult
            {
                QueryType = "Reverse Lookup",
                Query = ipAddress,
                HostName = hostEntry.HostName,
                Status = "Success",
                Timestamp = DateTime.Now
            };

            result.IpAddresses.Add(new IpAddressInfo
            {
                Address = ipAddress,
                AddressFamily = address.AddressFamily.ToString()
            });

            // Add aliases if any
            foreach (var alias in hostEntry.Aliases)
            {
                result.Aliases.Add(alias);
            }

            Results.Add(result);
            StatusMessage = $"Reverse lookup successful: {ipAddress} → {hostEntry.HostName}";
            
            _logger.LogInformation("DNS reverse lookup successful for {IpAddress} → {HostName}", 
                ipAddress, hostEntry.HostName);
        }
        catch (Exception ex)
        {
            var errorResult = new DnsResult
            {
                QueryType = "Reverse Lookup",
                Query = ipAddress,
                Status = "Failed",
                ErrorMessage = ex.Message,
                Timestamp = DateTime.Now
            };
            
            Results.Add(errorResult);
            StatusMessage = $"Reverse lookup failed for {ipAddress}";
            
            _logger.LogWarning(ex, "DNS reverse lookup failed for {IpAddress}", ipAddress);
        }
    }

    private static bool IsValidIpAddress(string input)
    {
        return IPAddress.TryParse(input, out _);
    }
}
