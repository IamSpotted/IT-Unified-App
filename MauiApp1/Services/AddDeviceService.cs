using System.Diagnostics;
using System.Data.SqlClient;
using MauiApp1.Interfaces;
using MauiApp1.Models;
using DeviceModel = MauiApp1.Models.Device;
using System.Data;

namespace MauiApp1.Services;

public class AddDeviceService : IAddDeviceService
{
    private readonly ILogger<AddDeviceService> _logger;
    private readonly SecureCredentialsService _credentialsService;

    public AddDeviceService(ILogger<AddDeviceService> logger, SecureCredentialsService credentialsService)
    {
        _logger = logger;
        _credentialsService = credentialsService;
    }

    /// <summary>
    /// Adds a new device to the database
    /// </summary>
    public async Task<bool> AddDeviceAsync(DeviceModel device, string deviceType = "Other")
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
                _logger.LogError("No database credentials available for AddDevice");
                return false;
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            var devicesQuery = SQLQueryService.AddDeviceQuery;

            try
            {
                // Insert into devices table (all columns)
                using var devicesCommand = new SqlCommand(devicesQuery, connection, transaction);

                devicesCommand.Parameters.Add(new SqlParameter("@hostname", SqlDbType.NVarChar, 510) { Value = device.Hostname });
                devicesCommand.Parameters.Add(new SqlParameter("@serial_number", SqlDbType.NVarChar, 200) { Value = device.SerialNumber ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@asset_tag", SqlDbType.NVarChar, 200) { Value = device.AssetTag ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@domain_name", SqlDbType.NVarChar, 200) { Value = device.DomainName ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@workgroup", SqlDbType.NVarChar, 200) { Value = device.Workgroup ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@is_domain_joined", SqlDbType.Bit) { Value = device.IsDomainJoined == true ? 1 : 0 });
                devicesCommand.Parameters.Add(new SqlParameter("@manufacturer", SqlDbType.NVarChar, 200) { Value = device.Manufacturer ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@model", SqlDbType.NVarChar, 200) { Value = device.Model ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@cpu_info", SqlDbType.NVarChar, 510) { Value = device.CpuInfo ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@total_ram_gb", SqlDbType.Int) { Value = device.TotalRamGb });
                devicesCommand.Parameters.Add(new SqlParameter("@ram_type", SqlDbType.NVarChar, 200) { Value = device.RamType ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@storage_info", SqlDbType.NVarChar, -1) { Value = device.StorageInfo ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@bios_version", SqlDbType.NVarChar, 200) { Value = device.BiosVersion ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@os_name", SqlDbType.NVarChar, 200) { Value = device.OsName ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@os_version", SqlDbType.NVarChar, 200) { Value = device.OSVersion ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@os_architecture", SqlDbType.NVarChar, 200) { Value = device.OsArchitecture ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@primary_ip", SqlDbType.NVarChar, 200) { Value = device.PrimaryIp ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@primary_mac", SqlDbType.NVarChar, 200) { Value = device.PrimaryMac ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@secondary_ips", SqlDbType.NVarChar, -1) { Value = device.SecondaryIps ?? (object)DBNull.Value }); // nvarchar(max)
                devicesCommand.Parameters.Add(new SqlParameter("@secondary_macs", SqlDbType.NVarChar, -1) { Value = device.SecondaryMacs ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@dns_servers", SqlDbType.NVarChar, -1) { Value = device.DnsServers ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@default_gateways", SqlDbType.NVarChar, -1) { Value = device.DefaultGateways ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@subnet_masks", SqlDbType.NVarChar, -1) { Value = device.SubnetMasks ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@area", SqlDbType.NVarChar, 200) { Value = device.Area ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@zone", SqlDbType.NVarChar, 200) { Value = device.Zone ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@line", SqlDbType.NVarChar, 200) { Value = device.Line ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@pitch", SqlDbType.NVarChar, 200) { Value = device.Pitch ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@floor", SqlDbType.NVarChar, 200) { Value = device.Floor ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@pillar", SqlDbType.NVarChar, 200) { Value = device.Pillar ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@additional_notes", SqlDbType.NVarChar, -1) { Value = device.AdditionalNotes ?? (object)DBNull.Value }); // nvarchar(max)
                devicesCommand.Parameters.Add(new SqlParameter("@created_at", SqlDbType.DateTime) { Value = device.CreatedAt ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@updated_at", SqlDbType.DateTime) { Value = device.UpdatedAt ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@last_discovered", SqlDbType.DateTime) { Value = device.LastDiscovered ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@discovery_method", SqlDbType.NVarChar, 200) { Value = device.DiscoveryMethod ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@device_type", SqlDbType.NVarChar, 200) { Value = device.DeviceType ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@web_interface_url", SqlDbType.NVarChar, 200) { Value = device.WebInterfaceUrl ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@equipment_group", SqlDbType.NVarChar, 200) { Value = device.EquipmentGroup ?? (object)DBNull.Value });

                var newDeviceId = await devicesCommand.ExecuteScalarAsync();
                if (newDeviceId == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                device.device_id = (int)newDeviceId;

                await transaction.CommitAsync();
                _logger.LogInformation("Added device {DeviceName} to database with type {DeviceType} and ID {device_id}",
                    device.Hostname, deviceType, device.device_id);
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
            _logger.LogError(ex, "Error adding device {DeviceName} to database", device.Hostname);
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
