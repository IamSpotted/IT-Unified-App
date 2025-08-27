using System.Diagnostics;
using MauiApp1.Interfaces;
using MauiApp1.Models;
using DeviceModel = MauiApp1.Models.Device;

namespace MauiApp1.Services;

/// <summary>
/// Service for establishing remote connections to PC devices using Netop
/// </summary>
public class NetopConnectService : INetopConnectService
{
    private readonly ILogger<NetopConnectService> _logger;
    private readonly ISettingsService _settingsService;
    private List<DeviceModel> _devices = new();

    public NetopConnectService(ILogger<NetopConnectService> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _devices = new List<DeviceModel>(); // Start with empty list - no mock data
        _logger.LogInformation("NetopConnectService initialized with empty device list");
    }

    /// <summary>
    /// Establishes a remote connection to the specified PC device
    /// </summary>
    /// <param name="device">The PC device to connect to</param>
    /// <returns>True if connection was initiated successfully, false otherwise</returns>
    public async Task<bool> ConnectToDeviceAsync(Netop device)
    {
        try
        {
            if (device == null)
            {
                _logger.LogError("Device cannot be null");
                return false;
            }

            if (device.DeviceType != "PC" && device.DeviceType != "Server")
            {
                _logger.LogError("Netop connections are only supported for PC and Server devices. Device type: {device_type}", device.DeviceType);
                return false;
            }

            if (string.IsNullOrEmpty(device.Hostname))
            {
                _logger.LogError("Device hostname is required for Netop connection. Device: {Hostname}", device.Hostname);
                return false;
            }

            if (!await IsNetopAvailableAsync())
            {
                _logger.LogError("Netop is not available or not configured properly");
                return false;
            }

#if WINDOWS
            var netopPath = _settingsService.GetAsync<string>("NetopServicePath", string.Empty).GetAwaiter().GetResult();
            var processStartInfo = new ProcessStartInfo
            {
                FileName = netopPath,
                Arguments = $"/C:TCP/IP /H:{device.Hostname}",
                UseShellExecute = true
            };

            _logger.LogInformation("Starting Netop connection to {Hostname}", device.Hostname);
            
            var process = Process.Start(processStartInfo);
            
            if (process != null)
            {
                _logger.LogInformation("Successfully initiated Netop connection to {Hostname}", device.Hostname);
                return true;
            }
            else
            {
                _logger.LogError("Failed to start Netop process for device {Hostname}", device.Hostname);
                return false;
            }
#else
            _logger.LogWarning("Netop connections are only supported on Windows platform");
            return false;
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Netop connection to device {Hostname}: {ErrorMessage}", 
                device?.Hostname ?? "Unknown", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Checks if Netop is available and configured properly
    /// </summary>
    /// <returns>True if Netop service is available</returns>
    public bool IsNetopAvailable()
    {
        return IsNetopAvailableAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Checks if Netop is available and configured properly (async version)
    /// </summary>
    /// <returns>True if Netop service is available</returns>
    private async Task<bool> IsNetopAvailableAsync()
    {
        try
        {
            var netopPath = await _settingsService.GetAsync<string>("NetopServicePath", string.Empty);
            if (string.IsNullOrEmpty(netopPath))
            {
                _logger.LogWarning("Netop service path not configured in settings");
                return false;
            }

#if WINDOWS
            if (!File.Exists(netopPath))
            {
                _logger.LogWarning("Netop executable not found at path: {NetopPath}", netopPath);
                return false;
            }

            return true;
#else
            _logger.LogWarning("Netop connections are only supported on Windows platform");
            return false;
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Netop availability: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public Task<List<DeviceModel>> GetDevicesAsync()
    {
        return Task.FromResult(_devices);
    }


    public void ConnectToDevice(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname)) throw new ArgumentNullException(nameof(hostname));

        var netopPath = _settingsService.GetAsync<string>("NetopServicePath", string.Empty).GetAwaiter().GetResult();
        var processStartInfo = new ProcessStartInfo
        {
            FileName = netopPath,
            Arguments = $"/C:TCP/IP /H:{hostname}",
            UseShellExecute = true
        };

        try
        {
            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start Netop connection: {ex.Message}");
        }
    }
}
