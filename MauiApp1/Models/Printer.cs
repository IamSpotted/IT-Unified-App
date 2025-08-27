namespace MauiApp1.Models;

/// <summary>
/// Represents a printer in the system
/// </summary>
public class Printer : IFilterable
{
    public string? Hostname { get; set; } = string.Empty;
    public string? PrimaryIp { get; set; } = string.Empty;
    public string? Area { get; set; } = string.Empty;
    public string? Zone { get; set; } = string.Empty;
    public string? Line { get; set; } = string.Empty;
    public string? Pitch { get; set; } = string.Empty;
    public string? Column { get; set; } = string.Empty;
    public string? Level { get; set; } = string.Empty;
    public string? Model { get; set; } = string.Empty;
    public string? SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full location string for display
    /// </summary>
    public string FullLocation => $"{Area}-{Zone}-{Line}-{Column}-{Level}".Trim('-');

    /// <summary>
    /// Gets the web interface URL for the printer
    /// </summary>
    public string WebInterfaceUrl => $"http://{PrimaryIp}";

    /// <summary>
    /// Gets the display text for the printer button
    /// </summary>
    public string DisplayText => $"{Hostname}\n{PrimaryIp}\n{FullLocation}";

    /// <summary>
    /// Gets the printer type icon
    /// </summary>
    public string PrinterIcon => "🖨️";

    public string GetFilterValue(string filterName)
    {
        return filterName switch
        {
            "Area" => Area ?? string.Empty,
            "Zone" => Zone ?? string.Empty,
            "Line" => Line ?? string.Empty,
            "Pitch" => Pitch ?? string.Empty,
            "Column" => Column ?? string.Empty,
            "Level" => Level ?? string.Empty,
            "Model" => Model ?? string.Empty,
            "Serial Number" => SerialNumber ?? string.Empty,
            _ => string.Empty
        };
    }

    public bool MatchesFilter(string filterName, string filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
            return true;

        var actualValue = GetFilterValue(filterName);
        return string.Equals(actualValue, filterValue, StringComparison.OrdinalIgnoreCase);
    }
}
