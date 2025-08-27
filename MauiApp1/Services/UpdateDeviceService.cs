using System.Diagnostics;
using System.Data.SqlClient;
using MauiApp1.Interfaces;
using MauiApp1.Models;
using DeviceModel = MauiApp1.Models.Device;

namespace MauiApp1.Services;

public class UpdateDeviceService : IUpdateDeviceService
{
    private readonly ILogger<UpdateDeviceService> _logger;
    private readonly SecureCredentialsService _credentialsService;

    public UpdateDeviceService(ILogger<UpdateDeviceService> logger, SecureCredentialsService credentialsService)
    {
        _logger = logger;
        _credentialsService = credentialsService;
    }

    /// <summary>
    /// Updates an existing device in the database
    /// </summary>
    public async Task<bool> UpdateDeviceAsync(DeviceModel device, string deviceType = "Other")
    {
        try
        {
            // Sanitize all input data to prevent SQL injection
            InputSanitizer.SanitizeDevice(device);
            var sanitizedDeviceType = InputSanitizer.SanitizeDeviceType(deviceType);

            // Required field validation
            if (string.IsNullOrWhiteSpace(device.Hostname))
            {
                _logger.LogError("Hostname is required and cannot be empty.");
                return false;
            }

            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogError("No database credentials available for Update Device");
                return false;
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            var devicesQuery = SQLQueryService.UpdateDeviceQuery;

            try
            {
                // Insert into devices table (all columns)
                using var devicesCommand = new SqlCommand(devicesQuery, connection, transaction);

                devicesCommand.Parameters.AddWithValue("@hostname", device.Hostname);
                devicesCommand.Parameters.AddWithValue("@serial_number", device.SerialNumber ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@asset_tag", device.AssetTag ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@domain_name", device.DomainName ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@workgroup", device.Workgroup ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@is_domain_joined", device.IsDomainJoined == true ? 1 : 0);
                devicesCommand.Parameters.AddWithValue("@manufacturer", device.Manufacturer ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@model", device.Model ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@cpu_info", device.CpuInfo ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@total_ram_gb", device.TotalRamGb);
                devicesCommand.Parameters.AddWithValue("@ram_type", device.RamType ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@storage_info", device.StorageInfo ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@bios_version", device.BiosVersion ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_name", device.OsName ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_version", device.OSVersion ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_architecture", device.OsArchitecture ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_ip", device.PrimaryIp ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_mac", device.PrimaryMac ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@secondary_ips", device.SecondaryIps ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@secondary_macs", device.SecondaryMacs ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@dns_servers", device.DnsServers ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@default_gateways", device.DefaultGateways ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@subnet_masks", device.SubnetMasks ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_status", device.DeviceStatus ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@area", device.Area ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@zone", device.Zone ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@line", device.Line ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pitch", device.Pitch ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@floor", device.Floor ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pillar", device.Pillar ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@additional_notes", device.AdditionalNotes ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@created_at", device.CreatedAt ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@updated_at", device.UpdatedAt ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@last_discovered", device.LastDiscovered ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@discovery_method", device.DiscoveryMethod ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_type", sanitizedDeviceType);
                devicesCommand.Parameters.AddWithValue("@web_interface_url", device.WebInterfaceUrl ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@equipment_group", device.EquipmentGroup ?? (object)DBNull.Value);

                devicesCommand.Parameters.AddWithValue("@device_id", device.device_id);

                var rowsAffected = await devicesCommand.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }                

                await transaction.CommitAsync();
                _logger.LogInformation("Updated device {Hostname} in database with type {DeviceType} and ID {device_id}",
                    device.Hostname, device.device_type, device.device_id);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device {Hostname} in database", device.Hostname);
            return false;
        }
    }
    private static string BuildConnectionString(string server, string database, bool useWindowsAuth,
        string? username, string? password, int connectionTimeout = 30)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            ConnectTimeout = connectionTimeout,
            TrustServerCertificate = true, // For development environments
            Encrypt = false // Set to true for production with proper certificates
        };

        if (useWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = username ?? string.Empty;
            builder.Password = password ?? string.Empty;
        }

        return builder.ConnectionString;
    }
}
