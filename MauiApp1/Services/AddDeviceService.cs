using System.Diagnostics;
using System.Data.SqlClient;
using MauiApp1.Interfaces;
using MauiApp1.Models;

using System.Data;

namespace MauiApp1.Services;

public class AddDeviceService : IAddDeviceService
{
    private readonly ILogger<AddDeviceService> _logger;
    private readonly SecureCredentialsService _credentialsService;
    private readonly IDatabaseService _databaseService;

    public AddDeviceService(ILogger<AddDeviceService> logger, SecureCredentialsService credentialsService, IDatabaseService databaseService)
    {
        _logger = logger;
        _credentialsService = credentialsService;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Adds a device with duplicate checking and resolution options
    /// </summary>
    public async Task<DeviceAddResult> AddDeviceWithDuplicateCheckAsync(Models.Device device, string deviceType = "Other", bool checkDuplicates = true, DuplicateResolutionOptions? resolutionOptions = null)
    {
        var result = new DeviceAddResult();

        try
        {
            // Sanitize all input data to prevent SQL injection
            InputSanitizer.SanitizeDevice(device);
            var sanitizedDeviceType = InputSanitizer.SanitizeDeviceType(deviceType);

            // Required field validation
            if (string.IsNullOrWhiteSpace(device.Hostname))
            {
                result.Success = false;
                result.Message = "Hostname is required and cannot be empty.";
                result.ActionTaken = DeviceAddAction.Failed;
                _logger.LogError("Hostname is required and cannot be empty.");
                return result;
            }

            // Check for duplicates if requested
            if (checkDuplicates)
            {
                var duplicateResult = await _databaseService.CheckForDuplicateDevicesAsync(device);
                
                if (duplicateResult.HasDuplicates)
                {
                    result.DuplicatesFound = true;
                    result.DuplicateDetectionResult = duplicateResult;

                    // If no resolution options provided, return with duplicate information for user decision
                    if (resolutionOptions == null)
                    {
                        result.Success = false;
                        result.Message = $"Found {duplicateResult.PotentialDuplicates.Count} potential duplicate(s). User action required.";
                        result.ActionTaken = DeviceAddAction.Cancelled;
                        
                        var duplicateDetails = string.Join(", ", 
                            duplicateResult.MatchDetails.Take(3).Select(md => 
                                $"{md.ExistingDevice.Hostname} ({md.MatchReason})"));
                        
                        _logger.LogInformation("Duplicate devices found for {Hostname}: {DuplicateDetails}", 
                            device.Hostname, duplicateDetails);
                        
                        return result;
                    }

                    // Handle the duplicate based on resolution options
                    switch (resolutionOptions.Action)
                    {
                        case DuplicateResolutionAction.Cancel:
                            result.Success = false;
                            result.Message = "Device addition cancelled due to duplicates.";
                            result.ActionTaken = DeviceAddAction.Cancelled;
                            return result;

                        case DuplicateResolutionAction.CreateNew:
                            // Proceed with normal addition (ignore duplicates)
                            _logger.LogInformation("Creating new device despite duplicates for {Hostname} as per user choice", device.Hostname);
                            break;

                        case DuplicateResolutionAction.UpdateExisting:
                            if (resolutionOptions.SelectedExistingDevice != null)
                            {
                                var updateSuccess = await _databaseService.UpdateDeviceAsync(device, sanitizedDeviceType);
                                if (updateSuccess)
                                {
                                    result.Success = true;
                                    result.Message = $"Successfully updated existing device: {device.Hostname}";
                                    result.ActionTaken = DeviceAddAction.Updated;
                                    result.DeviceId = resolutionOptions.SelectedExistingDevice.device_id;
                                    
                                    await _databaseService.LogAuditEntryAsync(resolutionOptions.SelectedExistingDevice.device_id, 
                                        "UPDATE", "DEVICE_REFRESH", "Manual device update", 
                                        $"Device updated via duplicate resolution: {resolutionOptions.ResolutionReason}");
                                }
                                else
                                {
                                    result.Success = false;
                                    result.Message = "Failed to update existing device.";
                                    result.ActionTaken = DeviceAddAction.Failed;
                                }
                                return result;
                            }
                            break;

                        case DuplicateResolutionAction.MergeData:
                            if (resolutionOptions.SelectedExistingDevice != null)
                            {
                                var mergeSuccess = await _databaseService.MergeDeviceDataAsync(
                                    resolutionOptions.SelectedExistingDevice, device, resolutionOptions);
                                    
                                if (mergeSuccess)
                                {
                                    result.Success = true;
                                    result.Message = $"Successfully merged device data for: {device.Hostname}";
                                    result.ActionTaken = DeviceAddAction.Merged;
                                    result.DeviceId = resolutionOptions.SelectedExistingDevice.device_id;
                                }
                                else
                                {
                                    result.Success = false;
                                    result.Message = "Failed to merge device data.";
                                    result.ActionTaken = DeviceAddAction.Failed;
                                }
                                return result;
                            }
                            break;
                    }
                }
            }

            // Add the device normally (either no duplicates found or user chose to create new)
            var addSuccess = await _databaseService.AddDeviceAsync(device, sanitizedDeviceType);
            
            if (addSuccess)
            {
                result.Success = true;
                result.Message = $"Successfully added device: {device.Hostname}";
                result.ActionTaken = DeviceAddAction.Added;
                result.DeviceId = device.device_id;
                
                _logger.LogInformation("Added new device: {DeviceName} with type {DeviceType} and ID {DeviceId}",
                    device.Hostname, sanitizedDeviceType, device.device_id);
            }
            else
            {
                result.Success = false;
                result.Message = "Failed to add device to database.";
                result.ActionTaken = DeviceAddAction.Failed;
                _logger.LogError("Failed to add device: {DeviceName}", device.Hostname);
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error occurred while adding device: {ex.Message}";
            result.ActionTaken = DeviceAddAction.Failed;
            result.ErrorDetails = ex.ToString();
            _logger.LogError(ex, "Error in AddDeviceWithDuplicateCheckAsync for device: {DeviceName}", device.Hostname);
            return result;
        }
    }

    /// <summary>
    /// Adds a new device to the database (original method for backwards compatibility)
    /// </summary>
    public async Task<bool> AddDeviceAsync(Models.Device device, string deviceType = "Other")
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

            _logger.LogInformation("AddDeviceService: Connecting to Server='{Server}', Database='{Database}'", 
                credentials.Server, credentials.Database);

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
                devicesCommand.Parameters.Add(new SqlParameter("@nic2_ip", SqlDbType.NVarChar, 200) { Value = device.Nic2Ip ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@nic2_mac", SqlDbType.NVarChar, 200) { Value = device.Nic2Mac ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@nic3_ip", SqlDbType.NVarChar, 200) { Value = device.Nic3Ip ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@nic3_mac", SqlDbType.NVarChar, 200) { Value = device.Nic3Mac ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@nic4_ip", SqlDbType.NVarChar, 200) { Value = device.Nic4Ip ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@nic4_mac", SqlDbType.NVarChar, 200) { Value = device.Nic4Mac ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@primary_dns", SqlDbType.NVarChar, 200) { Value = device.PrimaryDns ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@secondary_dns", SqlDbType.NVarChar, 200) { Value = device.SecondaryDns ?? (object)DBNull.Value });
                devicesCommand.Parameters.Add(new SqlParameter("@primary_subnet", SqlDbType.NVarChar, 200) { Value = device.PrimarySubnet ?? (object)DBNull.Value });
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

    /// <summary>
    /// Adds a device with audit logging support
    /// </summary>
    public async Task<bool> AddDeviceAsync(Models.Device device, string deviceType, string applicationUser, Guid? discoverySessionId, string changeReason)
    {
        try
        {
            // Use the enhanced DatabaseService method
            var success = await _databaseService.AddDeviceAsync(device, deviceType, applicationUser, discoverySessionId, changeReason);
            
            if (success)
            {
                _logger.LogInformation("Successfully added device '{hostname}' (ID: {device_id}) with audit logging. Session: {sessionId}, Reason: {reason}", 
                    device.Hostname, device.device_id, discoverySessionId, changeReason);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add device '{hostname}' with audit logging", device.Hostname);
            return false;
        }
    }
}
