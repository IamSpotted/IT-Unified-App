using System.Collections.ObjectModel;

namespace MauiApp1.Models;

public class DnsResult
{
    public string QueryType { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public ObservableCollection<IpAddressInfo> IpAddresses { get; } = new();
    public ObservableCollection<string> Aliases { get; } = new();

    public bool IsSuccess => Status == "Success";
    public bool HasMultipleIpAddresses => IpAddresses.Count > 1;
    public bool HasAliases => Aliases.Any();
    public string StatusColor => Status switch
    {
        "Success" => "#4CAF50",
        "Failed" => "#F44336",
        "Error" => "#FF5722",
        _ => "#9E9E9E"
    };

    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");
}

public class IpAddressInfo
{
    public string Address { get; set; } = string.Empty;
    public string AddressFamily { get; set; } = string.Empty;
    
    public string DisplayText => $"{Address} ({AddressFamily})";
}
