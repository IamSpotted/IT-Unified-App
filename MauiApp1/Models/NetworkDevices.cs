namespace MauiApp1.Models;

/// <summary>
/// Represents a networkDevice in the system
/// </summary>
public class NetworkDevice : IFilterable
{
    public string Hostname { get; set; } = string.Empty;
    
    // Location/Organization fields for filtering
    public string? Area { get; set; } = string.Empty;
    public string? Zone { get; set; } = string.Empty;
    public string? Line { get; set; } = string.Empty;
    public string? Column { get; set; } = string.Empty;
    public string? Level { get; set; } = string.Empty;
    public string? Model { get; set; } = string.Empty;
    public string? PrimaryIp { get; set; } = string.Empty;

    // NetworkDevice web interface URLs based on IP address
    public string WebInterfaceUrl => $"http://{PrimaryIp}/#view";
    public string PreviewUrl => $"http://{PrimaryIp}/preview";
    
    // Full location description for display
    public string FullLocation => $"{Area}/{Zone}/{Line}/{Column}/{Level}".Trim('/');

    // IFilterable implementation
    public string GetFilterValue(string filterProperty)
    {
        return filterProperty.ToLowerInvariant() switch
        {
            "area" => Area ?? string.Empty,
            "zone" => Zone ?? string.Empty,
            "line" => Line ?? string.Empty,
            "column" => Column ?? string.Empty,
            "level" => Level ?? string.Empty,
            "model" => Model ?? string.Empty,
            "name" => Hostname ?? string.Empty,
            "ipaddress" => PrimaryIp ?? string.Empty,
            _ => string.Empty
        };
    }

    public bool MatchesFilter(string filterProperty, string filterValue)
    {
        if (string.IsNullOrEmpty(filterValue))
            return true;

        var actualValue = GetFilterValue(filterProperty);
        return string.Equals(actualValue, filterValue, StringComparison.OrdinalIgnoreCase);
    }
}
