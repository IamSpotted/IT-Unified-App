namespace MauiApp1.Models;

/// <summary>
/// The device type of a Netop device.
/// </summary>
public enum NetopDeviceType
{
    Router,
    Switch,
    PC,
    Server
}

/// <summary>
/// Represents a Netop device in the system.
/// </summary>
public class Netop : IFilterable
{
    public string Hostname { get; set; } = string.Empty;

    /// <summary>
    /// Raw device type string from the database.
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Parsed and strongly-typed version of DeviceType.
    /// Returns null if parsing fails.
    /// </summary>
    public NetopDeviceType? DeviceTypeEnum
    {
        get
        {
            return Enum.TryParse<NetopDeviceType>(DeviceType, ignoreCase: true, out var result)
                ? result
                : (NetopDeviceType?)null;
        }
    }

    // Location/Organization fields for filtering
    public string? Area { get; set; } = string.Empty;
    public string? Zone { get; set; } = string.Empty;
    public string? Line { get; set; } = string.Empty;
    public string? Pitch { get; set; } = string.Empty;
    public string? Column { get; set; } = string.Empty;
    public string? Level { get; set; } = string.Empty;
    public string? Model { get; set; } = string.Empty;
    public string? PrimaryIp { get; set; } = string.Empty;

    // Netop web interface URLs based on IP address
    public string WebInterfaceUrl => $"http://{PrimaryIp}";
    public string PreviewUrl => $"http://{PrimaryIp}";

    // Full location description for display
    public string FullLocation => $"{Area}/{Zone}/{Line}/{Column}/{Level}".Trim('/');

    // IFilterable implementation
    public string GetFilterValue(string filterProperty)
    {
        return filterProperty.ToLowerInvariant() switch
        {
            "hostname" => Hostname ?? string.Empty,
            "devicetype" => DeviceType ?? string.Empty,
            "area" => Area ?? string.Empty,
            "zone" => Zone ?? string.Empty,
            "line" => Line ?? string.Empty,
            "pitch" => Pitch ?? string.Empty,
            "column" => Column ?? string.Empty,
            "level" => Level ?? string.Empty,
            "model" => Model ?? string.Empty,
            "name" => Hostname ?? string.Empty,
            "primary_ip" => PrimaryIp ?? string.Empty,
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