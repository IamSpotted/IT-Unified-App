using System.Text.RegularExpressions;

namespace MauiApp1.Services;

/// <summary>
/// Provides input sanitization methods to prevent SQL injection and other security vulnerabilities
/// </summary>
public static class InputSanitizer
{
    // Maximum field lengths to prevent buffer overflow attacks
    private const int MAX_STRING_LENGTH = 255;
    private const int MAX_NOTES_LENGTH = 2000;
    private const int MAX_URL_LENGTH = 500;

    // Regex patterns for validation
    private static readonly Regex ValidDeviceTypePattern = new(@"^[a-zA-Z]+$", RegexOptions.Compiled);
    private static readonly Regex ValidDeviceStatusPattern = new(@"^[a-zA-Z]+$", RegexOptions.Compiled);
    private static readonly Regex ValidIPAddressPattern = new(@"^(\d{1,3}\.){3}\d{1,3}$", RegexOptions.Compiled);
    private static readonly Regex ValidHostnamePattern = new(@"^[a-zA-Z0-9\-_.]+$", RegexOptions.Compiled);
    private static readonly Regex ValidAlphanumericPattern = new(@"^[a-zA-Z0-9\s\-_.#\/]+$", RegexOptions.Compiled);
    private static readonly Regex ValidUrlPattern = new(@"^https?:\/\/[a-zA-Z0-9\-._~:\/?#[\]@!$&'()*+,;=]+$", RegexOptions.Compiled);

    // Whitelisted device types
    private static readonly HashSet<string> ValidDeviceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Printer", "Camera", "PC", "Server", "Router", "Switch", "Other"
    };

    private static readonly HashSet<string> ValidDeviceStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active", "Inactive", "Maintenance", "Missing", "Retired"
    };

    public static IReadOnlyList<string> DeviceTypeOptions => ValidDeviceTypes.ToList();

    public static IReadOnlyList<string> DeviceStatusOptions => ValidDeviceStatus.ToList();

    // SQL injection patterns to detect
    private static readonly string[] SqlInjectionPatterns = {
        "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "EXEC", "EXECUTE",
        "UNION", "SCRIPT", "DECLARE", "CAST", "CONVERT", "SUBSTRING", "ASCII", "CHAR",
        "--", "/*", "*/", "xp_", "sp_", "@@", "INFORMATION_SCHEMA"
    };

    /// <summary>
    /// Sanitizes a string input by removing potentially dangerous characters and limiting length
    /// </summary>
    public static string SanitizeString(string? input, int maxLength = MAX_STRING_LENGTH)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove null characters and control characters except newlines/tabs
        var sanitized = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
        
        // Trim whitespace
        sanitized = sanitized.Trim();

        // Truncate to max length
        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        // Check for SQL injection patterns
        if (ContainsSqlInjectionPattern(sanitized))
        {
            throw new ArgumentException($"Input contains potentially dangerous content: {input}");
        }

        return sanitized;
    }

    /// <summary>
    /// Validates and sanitizes device type input
    /// </summary>
    public static string SanitizeDeviceType(string? deviceType)
    {
        if (string.IsNullOrEmpty(deviceType))
            return "Type cannot be empty";

        var sanitized = SanitizeString(deviceType, 50);

        // Must be in whitelist
        if (!ValidDeviceTypes.Contains(sanitized))
        {
            return "Type not allowed";
        }

        // Additional pattern validation
        if (!ValidDeviceTypePattern.IsMatch(sanitized))
        {
            return "Pattern";
        }

        return sanitized;
    }

    public static string SanitizeDeviceStatus(string? deviceStatus)
    {
        if (string.IsNullOrEmpty(deviceStatus))
            return "Status cannot be empty";

        var sanitized = SanitizeString(deviceStatus, 50);

        // Must be in whitelist
        if (!ValidDeviceStatus.Contains(sanitized))
        {
            return "Status not allowed";
        }

        // Additional pattern validation
        if (!ValidDeviceStatusPattern.IsMatch(sanitized))
        {
            return "Pattern";
        }

        return sanitized;
    }

    /// <summary>
    /// Validates and sanitizes IP address input
    /// </summary>
    public static string? SanitizeIPAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return null;

        var sanitized = SanitizeString(ipAddress, 15);

        if (!ValidIPAddressPattern.IsMatch(sanitized))
        {
            throw new ArgumentException($"Invalid IP address format: {ipAddress}");
        }

        // Additional validation - check each octet
        var octets = sanitized.Split('.');
        foreach (var octet in octets)
        {
            if (!int.TryParse(octet, out var value) || value < 0 || value > 255)
            {
                throw new ArgumentException($"Invalid IP address format: {ipAddress}");
            }
        }

        return sanitized;
    }

    /// <summary>
    /// Validates and sanitizes hostname input
    /// </summary>
    public static string SanitizeHostname(string? hostname)
    {
        if (string.IsNullOrEmpty(hostname))
            return string.Empty;

        var sanitized = SanitizeString(hostname, 63); // DNS hostname limit

        if (!ValidHostnamePattern.IsMatch(sanitized))
        {
            throw new ArgumentException($"Invalid hostname format: {hostname}");
        }

        return sanitized;
    }

    /// <summary>
    /// Validates and sanitizes general alphanumeric fields (Area, Line, Pitch, etc.)
    /// </summary>
    public static string SanitizeAlphanumeric(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sanitized = SanitizeString(input, 100);

        if (!ValidAlphanumericPattern.IsMatch(sanitized))
        {
            throw new ArgumentException($"Invalid characters in field: {input}");
        }

        return sanitized;
    }

    /// <summary>
    /// Validates and sanitizes URL input
    /// </summary>
    public static string? SanitizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        var sanitized = SanitizeString(url, MAX_URL_LENGTH);

        if (!ValidUrlPattern.IsMatch(sanitized))
        {
            throw new ArgumentException($"Invalid URL format: {url}");
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes notes/comments fields with larger length limit
    /// </summary>
    public static string? SanitizeNotes(string? notes)
    {
        if (string.IsNullOrEmpty(notes))
            return null;

        return SanitizeString(notes, MAX_NOTES_LENGTH);
    }

    /// <summary>
    /// Checks if input contains potential SQL injection patterns
    /// </summary>
    private static bool ContainsSqlInjectionPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var upperInput = input.ToUpperInvariant();

        return SqlInjectionPatterns.Any(pattern => upperInput.Contains(pattern));
    }

    /// <summary>
    /// Sanitizes a complete Device object
    /// </summary>
    public static void SanitizeDevice(Models.Device device)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));
            
        device.device_type = SanitizeAlphanumeric(device.device_type);
        device.Department = SanitizeAlphanumeric(device.Department);
        device.Location = SanitizeString(device.Location);
        device.Hostname = SanitizeHostname(device.Hostname);
        device.Area = SanitizeAlphanumeric(device.Area);
        device.Zone = SanitizeAlphanumeric(device.Zone);
        device.Line = SanitizeAlphanumeric(device.Line);
        device.Column = SanitizeAlphanumeric(device.Column);
        device.Level = SanitizeAlphanumeric(device.Level);
        device.Pitch = SanitizeAlphanumeric(device.Pitch);
        device.SerialNumber = device.SerialNumber != null ? SanitizeAlphanumeric(device.SerialNumber) : null;
        device.AssetTag = device.AssetTag != null ? SanitizeAlphanumeric(device.AssetTag) : null;
        device.MacAddress = device.MacAddress != null ? SanitizeAlphanumeric(device.MacAddress) : null;
        device.Manufacturer = device.Manufacturer != null ? SanitizeString(device.Manufacturer) : null;
        device.Model = device.Model != null ? SanitizeString(device.Model) : null;
        device.AdditionalNotes = SanitizeNotes(device.AdditionalNotes);
        device.WebLink = SanitizeUrl(device.WebLink);
        device.WebLinkName = device.WebLinkName != null ? SanitizeString(device.WebLinkName) : null;
        device.IpAddress = SanitizeIPAddress(device.IpAddress);
    }
}
