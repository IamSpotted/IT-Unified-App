namespace MauiApp1.Models;

/// <summary>
/// Result of a database connection test operation.
/// </summary>
public class DatabaseTestResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public TimeSpan? ConnectionTime { get; set; }
    public string ServerInfo { get; set; } = string.Empty;
    public DateTime TestTimestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Information about a SQL database.
/// </summary>
public class DatabaseInfo
{
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string ServerVersion { get; set; } = string.Empty;
    public string ProductVersion { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public int CompatibilityLevel { get; set; }
    public DateTime CreatedDate { get; set; }
    public string Collation { get; set; } = string.Empty;
    public long SizeInMB { get; set; }
    public int UserTables { get; set; }
    public bool IsOnline { get; set; }
}
