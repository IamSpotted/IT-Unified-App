using System.Data.SqlClient;
using MauiApp1.Interfaces;
using MauiApp1.Models;
using Microsoft.Extensions.Logging;

namespace MauiApp1.Services
{
    public class RemoveDeviceService : IRemoveDeviceService
    {
        private readonly ILogger<RemoveDeviceService> _logger;
        private readonly IDatabaseService _databaseService;

        public RemoveDeviceService(ILogger<RemoveDeviceService> logger, IDatabaseService databaseService)
        {
            _logger = logger;
            _databaseService = databaseService;
        }

        public async Task<bool> RemoveDeviceAsync(string hostname)
        {
            return await RemoveDeviceAsync(hostname, $"Device '{hostname}' deleted via Database Admin interface");
        }

        public async Task<bool> RemoveDeviceAsync(string hostname, string deletionReason)
        {
            try
            {
                // First, get the device by hostname to find the device ID
                var devices = await _databaseService.SearchDevicesAsync(hostname);
                var device = devices?.FirstOrDefault(d => d.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase));
                
                if (device == null)
                {
                    _logger.LogWarning("Device with hostname '{hostname}' not found for removal", hostname);
                    return false;
                }

                // Use the simple DeleteDeviceAsync method - INSTEAD OF DELETE trigger will handle archiving
                // Pass the user-provided deletion reason for proper audit logging
                var success = await _databaseService.DeleteDeviceAsync(device.device_id, deletionReason);

                if (success)
                {
                    _logger.LogInformation("Successfully deleted device '{hostname}' (ID: {device_id}) - archived to deleted_devices", 
                        hostname, device.device_id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete device '{hostname}' (ID: {device_id})", 
                        hostname, device.device_id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing device '{hostname}'", hostname);
                return false;
            }
        }
    }
}