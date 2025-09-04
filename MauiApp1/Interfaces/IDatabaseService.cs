
using MauiApp1.Models;

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
    Task<List<Models.Device>> GetDevicesAsync(string? deviceType = null);
    Task<List<Models.Device>> SearchDevicesAsync(string searchQuery);
    Task<bool> AddDeviceAsync(Models.Device device, string deviceType = "Other");
    Task<bool> UpdateDeviceAsync(Models.Device device, string deviceType = "Other");
    
    // Enhanced methods with audit logging support
    Task<bool> AddDeviceAsync(Models.Device device, string deviceType, string applicationUser, Guid? discoverySessionId, string changeReason);
    Task<bool> UpdateDeviceAsync(Models.Device device, string deviceType, string applicationUser, Guid? discoverySessionId, string changeReason);
    Task<bool> DeleteDeviceAsync(int deviceId);
    Task<bool> DeleteDeviceAsync(int deviceId, string? deletionReason = null);
    Task<bool> ArchiveDeviceAsync(int deviceId, string? deletionReason = null);
    Task<bool> RestoreDeviceAsync(int deletedDeviceId, string? restoreReason = null);
    Task<bool> LogAuditEntryAsync(int deviceId, string actionType, string? fieldName = null, string? oldValue = null, string? newValue = null, string? changeReason = null);
    Task<List<Models.Device>> GetCamerasAsync();
    Task<List<Models.Device>> GetPrintersAsync();
    Task<List<Models.Device>> GetNetopsAsync();
    Task<List<Models.Device>> GetNetworkDevicesAsync();
    /// <summary>
    /// Gets a device from the database by hostname (returns null if not found)
    /// </summary>
    /// <param name="hostname">The device hostname to search for</param>
    /// <returns>The device if found, or null</returns>
    Task<Models.Device?> GetDeviceByHostnameAsync(string hostname);

    /// <summary>
    /// Gets devices from the database by IP address (any NIC)
    /// </summary>
    /// <param name="ipAddress">The IP address to search for</param>
    /// <returns>List of devices with matching IP address</returns>
    Task<List<Models.Device>> GetDevicesByIpAddressAsync(string ipAddress);

    /// <summary>
    /// Gets devices from the database by MAC address (any NIC)
    /// </summary>
    /// <param name="macAddress">The MAC address to search for</param>
    /// <returns>List of devices with matching MAC address</returns>
    Task<List<Models.Device>> GetDevicesByMacAddressAsync(string macAddress);

    /// <summary>
    /// Gets a device from the database by serial number
    /// </summary>
    /// <param name="serialNumber">The serial number to search for</param>
    /// <returns>The device if found, or null</returns>
    Task<Models.Device?> GetDeviceBySerialNumberAsync(string serialNumber);

    /// <summary>
    /// Gets a device from the database by asset tag
    /// </summary>
    /// <param name="assetTag">The asset tag to search for</param>
    /// <returns>The device if found, or null</returns>
    Task<Models.Device?> GetDeviceByAssetTagAsync(string assetTag);

    /// <summary>
    /// Checks for potential duplicate devices before adding a new device
    /// </summary>
    /// <param name="device">The device to check for duplicates</param>
    /// <returns>Duplicate detection result with potential matches</returns>
    Task<Models.DuplicateDetectionResult> CheckForDuplicateDevicesAsync(Models.Device device);

    /// <summary>
    /// Merges new device data with existing device data based on resolution options
    /// </summary>
    /// <param name="existingDevice">The existing device to update</param>
    /// <param name="newDevice">The new device data to merge</param>
    /// <param name="resolutionOptions">Options for how to resolve the merge</param>
    /// <returns>Success status of the merge operation</returns>
    Task<bool> MergeDeviceDataAsync(Models.Device existingDevice, Models.Device newDevice, Models.DuplicateResolutionOptions resolutionOptions);
    
}
