namespace MauiApp1.Models;

/// <summary>
/// Represents the current status of a network device from SolarWinds monitoring
/// </summary>
public class DeviceStatus
{
    /// <summary>
    /// Whether the device is currently online/reachable
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Response time in milliseconds (0 if offline)
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Status description from SolarWinds (e.g., "Up", "Down", "Warning")
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// When this status was last checked
    /// </summary>
    public DateTime LastChecked { get; set; }

    /// <summary>
    /// Gets a user-friendly status display string
    /// </summary>
    public string DisplayStatus => IsOnline ? $"🟢 Online ({ResponseTimeMs}ms)" : "🔴 Offline";

    /// <summary>
    /// Gets just the emoji indicator
    /// </summary>
    public string StatusEmoji => IsOnline ? "🟢" : "🔴";

    /// <summary>
    /// Gets the status text without emoji
    /// </summary>
    public string StatusText => IsOnline ? "Online" : "Offline";
}
