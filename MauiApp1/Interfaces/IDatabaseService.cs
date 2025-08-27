
using MauiApp1.Models;
using DeviceModel = MauiApp1.Models.Device;

namespace MauiApp1.Interfaces;

/// <summary>
/// Service interface for SQL database operations and connection testing.
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Tests the database connection using the provided credentials.
    /// </summary>
    /// <param name="server">Database server name or IP</param>
    /// <param name="database">Database name</param>
    /// <param name="useWindowsAuth">Whether to use Windows Authentication</param>
    /// <param name="username">SQL Server username (if not using Windows auth)</param>
    /// <param name="password">SQL Server password (if not using Windows auth)</param>
    /// <param name="searchQuery">Search query for device lookup</param>
    /// <param name="connectionTimeout">Connection timeout in seconds</param>
    /// <returns>Database connection test result</returns>
    Task<DatabaseTestResult> TestConnectionAsync(string server, string database, bool useWindowsAuth,
        string? username = null, string? password = null, int connectionTimeout = 30);

    /// <summary>
    /// Tests the database connection using stored credentials.
    /// </summary>
    /// <returns>Database connection test result</returns>
    Task<DatabaseTestResult> TestStoredConnectionAsync();

    /// <summary>
    /// Gets basic database information for the connected database.
    /// </summary>
    /// <returns>Database information</returns>
    Task<DatabaseInfo?> GetDatabaseInfoAsync();

    // Device CRUD Operations - Handles all device types (Printer, Camera, NetOp, Other) via DeviceType filtering
    Task<List<DeviceModel>> GetDevicesAsync(string? deviceType = null);
    Task<List<DeviceModel>> SearchDevicesAsync(string searchQuery);
    Task<bool> AddDeviceAsync(DeviceModel device, string deviceType = "Other");
    Task<bool> UpdateDeviceAsync(DeviceModel device, string deviceType = "Other");
    Task<bool> DeleteDeviceAsync(int deviceId);
    Task<List<DeviceModel>> GetCamerasAsync();
    Task<List<DeviceModel>> GetPrintersAsync();
    Task<List<DeviceModel>> GetNetopsAsync();
    /// <summary>
    /// Gets a device from the database by hostname (returns null if not found)
    /// </summary>
    /// <param name="hostname">The device hostname to search for</param>
    /// <returns>The device if found, or null</returns>
    Task<DeviceModel?> GetDeviceByHostnameAsync(string hostname);
    
}
