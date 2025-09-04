using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.Services;

/// <summary>
/// Service implementation for SQL database operations and connection testing.
/// </summary>
public class DatabaseService : IDatabaseService, ITransientService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly SecureCredentialsService _credentialsService;
    private readonly IAuthorizationService _authorizationService;

    public DatabaseService(ILogger<DatabaseService> logger, SecureCredentialsService credentialsService, IAuthorizationService authorizationService)
    {
        _logger = logger;
        _credentialsService = credentialsService;
        _authorizationService = authorizationService;
    }

    /// <inheritdoc />
    public async Task<DatabaseTestResult> TestConnectionAsync(string server, string database, bool useWindowsAuth,
        string? username = null, string? password = null, int connectionTimeout = 30)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Check authorization before attempting connection
            bool canConnect = await _authorizationService.CanConnectToDatabaseAsync(server);
            if (!canConnect)
            {
                stopwatch.Stop();

                bool isNonDomain = await _authorizationService.IsNonDomainEnvironmentAsync();
                string reason = isNonDomain
                    ? $"Non-domain environment: Only localhost database connections are allowed. Server '{server}' is blocked."
                    : $"Access denied: You don't have permission to connect to server '{server}'.";

                _logger.LogWarning("Database connection blocked for server {Server}: {Reason}", server, reason);

                return new DatabaseTestResult
                {
                    IsSuccess = false,
                    Message = $"🚫 {reason}",
                    ConnectionTime = stopwatch.Elapsed,
                    ErrorDetails = "ACCESS_DENIED"
                };
            }

            var connectionString = BuildConnectionString(server, database, useWindowsAuth, username, password, connectionTimeout);

            using var connection = new SqlConnection(connectionString);

            await connection.OpenAsync();

            // Get basic server information
            var serverInfo = await GetServerInfoAsync(connection);

            stopwatch.Stop();

            _logger.LogInformation("Successfully connected to database {Database} on server {Server} in {Duration}ms",
                database, server, stopwatch.ElapsedMilliseconds);

            return new DatabaseTestResult
            {
                IsSuccess = true,
                Message = $"✅ Successfully connected to '{database}' on '{server}'",
                ServerInfo = serverInfo,
                ConnectionTime = stopwatch.Elapsed,
                TestTimestamp = DateTime.Now
            };
        }
        catch (SqlException sqlEx)
        {
            stopwatch.Stop();
            _logger.LogError(sqlEx, "SQL error while testing connection to {Server}/{Database}", server, database);

            var userFriendlyMessage = GetUserFriendlySqlErrorMessage(sqlEx);

            return new DatabaseTestResult
            {
                IsSuccess = false,
                Message = $"❌ Connection failed: {userFriendlyMessage}",
                ErrorDetails = $"SQL Error {sqlEx.Number}: {sqlEx.Message}",
                ConnectionTime = stopwatch.Elapsed,
                TestTimestamp = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error while testing connection to {Server}/{Database}", server, database);

            return new DatabaseTestResult
            {
                IsSuccess = false,
                Message = $"❌ Connection failed: {ex.Message}",
                ErrorDetails = ex.ToString(),
                ConnectionTime = stopwatch.Elapsed,
                TestTimestamp = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Gets a device from the enhanced devices table by hostname (returns null if not found)
    /// </summary>
    public async Task<Models.Device?> GetDeviceByHostnameAsync(string hostname)
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetDeviceByHostnameAsync");
            return null;
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Use SqlQueryService for consistent query management
        using var command = new SqlCommand(SQLQueryService.GetDeviceByHostnameQuery, connection);
        command.Parameters.AddWithValue("@hostname", hostname);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Models.Device
            {
                device_id = GetInt32(reader, "device_id"),
                Hostname = GetStringOrEmpty(reader, "hostname"),
                SerialNumber = GetString(reader, "serial_number"),
                AssetTag = GetString(reader, "asset_tag"),
                device_type = string.Empty, // You may want to map this from another field
                Department = string.Empty,
                Location = string.Empty,
                Area = GetStringOrEmpty(reader, "area"),
                Zone = GetStringOrEmpty(reader, "zone"),
                Line = GetStringOrEmpty(reader, "line"),
                Column = GetStringOrEmpty(reader, "pillar"),
                Level = GetStringOrEmpty(reader, "floor"),
                Pitch = GetStringOrEmpty(reader, "pitch"),
                PrimaryMac = GetString(reader, "primary_mac"),
                PrimaryIp = GetString(reader, "primary_ip"),
                Manufacturer = GetString(reader, "manufacturer"),
                Model = GetString(reader, "model"),
                AdditionalNotes = GetString(reader, "additional_notes"),
                // Add more mappings as needed for your UI
            };
        }
        return null;
    }

    /// <summary>
    /// Gets devices from the database by IP address (any NIC)
    /// </summary>
    public async Task<List<Models.Device>> GetDevicesByIpAddressAsync(string ipAddress)
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetDevicesByIpAddressAsync");
            return new List<Models.Device>();
        }

        try
        {
            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT * FROM devices 
                WHERE primary_ip = @ipAddress 
                   OR nic2_ip = @ipAddress 
                   OR nic3_ip = @ipAddress 
                   OR nic4_ip = @ipAddress";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ipAddress", ipAddress);

            var devices = new List<Models.Device>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                devices.Add(MapDeviceFromReader(reader));
            }

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices by IP address: {IpAddress}", ipAddress);
            return new List<Models.Device>();
        }
    }

    /// <summary>
    /// Gets devices from the database by MAC address (any NIC)
    /// </summary>
    public async Task<List<Models.Device>> GetDevicesByMacAddressAsync(string macAddress)
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetDevicesByMacAddressAsync");
            return new List<Models.Device>();
        }

        try
        {
            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT * FROM devices 
                WHERE UPPER(REPLACE(primary_mac, ':', '')) = UPPER(REPLACE(@macAddress, ':', ''))
                   OR UPPER(REPLACE(nic2_mac, ':', '')) = UPPER(REPLACE(@macAddress, ':', ''))
                   OR UPPER(REPLACE(nic3_mac, ':', '')) = UPPER(REPLACE(@macAddress, ':', ''))
                   OR UPPER(REPLACE(nic4_mac, ':', '')) = UPPER(REPLACE(@macAddress, ':', ''))";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@macAddress", macAddress);

            var devices = new List<Models.Device>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                devices.Add(MapDeviceFromReader(reader));
            }

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices by MAC address: {MacAddress}", macAddress);
            return new List<Models.Device>();
        }
    }

    /// <summary>
    /// Gets a device from the database by serial number
    /// </summary>
    public async Task<Models.Device?> GetDeviceBySerialNumberAsync(string serialNumber)
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetDeviceBySerialNumberAsync");
            return null;
        }

        try
        {
            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT TOP 1 * FROM devices 
                WHERE UPPER(serial_number) = UPPER(@serialNumber)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@serialNumber", serialNumber);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapDeviceFromReader(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device by serial number: {SerialNumber}", serialNumber);
            return null;
        }
    }

    /// <summary>
    /// Gets a device from the database by asset tag
    /// </summary>
    public async Task<Models.Device?> GetDeviceByAssetTagAsync(string assetTag)
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetDeviceByAssetTagAsync");
            return null;
        }

        try
        {
            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT TOP 1 * FROM devices 
                WHERE UPPER(asset_tag) = UPPER(@assetTag)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@assetTag", assetTag);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapDeviceFromReader(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device by asset tag: {AssetTag}", assetTag);
            return null;
        }
    }

    /// <summary>
    /// Checks for potential duplicate devices before adding a new device
    /// </summary>
    public async Task<Models.DuplicateDetectionResult> CheckForDuplicateDevicesAsync(Models.Device device)
    {
        var result = new Models.DuplicateDetectionResult { NewDevice = device };
        var potentialDuplicates = new Dictionary<int, Models.Device>();
        var matchDetails = new List<Models.DuplicateMatchDetail>();

        try
        {
            // Check for hostname duplicates (highest priority)
            if (!string.IsNullOrWhiteSpace(device.Hostname))
            {
                var hostnameMatch = await GetDeviceByHostnameAsync(device.Hostname);
                if (hostnameMatch != null)
                {
                    potentialDuplicates[hostnameMatch.device_id] = hostnameMatch;
                    matchDetails.Add(new Models.DuplicateMatchDetail
                    {
                        ExistingDevice = hostnameMatch,
                        MatchedFields = new List<string> { "Hostname" },
                        MatchConfidence = Models.DuplicateMatchConfidence.High,
                        MatchReason = $"Exact hostname match: {device.Hostname}"
                    });
                }
            }

            // Check for serial number duplicates (high priority)
            if (!string.IsNullOrWhiteSpace(device.SerialNumber))
            {
                var serialMatch = await GetDeviceBySerialNumberAsync(device.SerialNumber);
                if (serialMatch != null && !potentialDuplicates.ContainsKey(serialMatch.device_id))
                {
                    potentialDuplicates[serialMatch.device_id] = serialMatch;
                    matchDetails.Add(new Models.DuplicateMatchDetail
                    {
                        ExistingDevice = serialMatch,
                        MatchedFields = new List<string> { "SerialNumber" },
                        MatchConfidence = Models.DuplicateMatchConfidence.High,
                        MatchReason = $"Serial number match: {device.SerialNumber}"
                    });
                }
            }

            // Check for asset tag duplicates (high priority)
            if (!string.IsNullOrWhiteSpace(device.AssetTag))
            {
                var assetMatch = await GetDeviceByAssetTagAsync(device.AssetTag);
                if (assetMatch != null && !potentialDuplicates.ContainsKey(assetMatch.device_id))
                {
                    potentialDuplicates[assetMatch.device_id] = assetMatch;
                    matchDetails.Add(new Models.DuplicateMatchDetail
                    {
                        ExistingDevice = assetMatch,
                        MatchedFields = new List<string> { "AssetTag" },
                        MatchConfidence = Models.DuplicateMatchConfidence.High,
                        MatchReason = $"Asset tag match: {device.AssetTag}"
                    });
                }
            }

            // Check for MAC address duplicates (medium priority)
            var macAddresses = new List<string>();
            if (!string.IsNullOrWhiteSpace(device.PrimaryMac)) macAddresses.Add(device.PrimaryMac);
            if (!string.IsNullOrWhiteSpace(device.Nic2Mac)) macAddresses.Add(device.Nic2Mac);
            if (!string.IsNullOrWhiteSpace(device.Nic3Mac)) macAddresses.Add(device.Nic3Mac);
            if (!string.IsNullOrWhiteSpace(device.Nic4Mac)) macAddresses.Add(device.Nic4Mac);

            foreach (var mac in macAddresses)
            {
                var macMatches = await GetDevicesByMacAddressAsync(mac);
                foreach (var macMatch in macMatches)
                {
                    if (!potentialDuplicates.ContainsKey(macMatch.device_id))
                    {
                        potentialDuplicates[macMatch.device_id] = macMatch;
                        matchDetails.Add(new Models.DuplicateMatchDetail
                        {
                            ExistingDevice = macMatch,
                            MatchedFields = new List<string> { "MAC Address" },
                            MatchConfidence = Models.DuplicateMatchConfidence.Medium,
                            MatchReason = $"MAC address match: {mac}"
                        });
                    }
                }
            }

            // Check for IP address duplicates (lower priority)
            var ipAddresses = new List<string>();
            if (!string.IsNullOrWhiteSpace(device.PrimaryIp)) ipAddresses.Add(device.PrimaryIp);
            if (!string.IsNullOrWhiteSpace(device.Nic2Ip)) ipAddresses.Add(device.Nic2Ip);
            if (!string.IsNullOrWhiteSpace(device.Nic3Ip)) ipAddresses.Add(device.Nic3Ip);
            if (!string.IsNullOrWhiteSpace(device.Nic4Ip)) ipAddresses.Add(device.Nic4Ip);

            foreach (var ip in ipAddresses)
            {
                var ipMatches = await GetDevicesByIpAddressAsync(ip);
                foreach (var ipMatch in ipMatches)
                {
                    if (!potentialDuplicates.ContainsKey(ipMatch.device_id))
                    {
                        potentialDuplicates[ipMatch.device_id] = ipMatch;
                        matchDetails.Add(new Models.DuplicateMatchDetail
                        {
                            ExistingDevice = ipMatch,
                            MatchedFields = new List<string> { "IP Address" },
                            MatchConfidence = Models.DuplicateMatchConfidence.Low,
                            MatchReason = $"IP address match: {ip}"
                        });
                    }
                }
            }

            result.PotentialDuplicates = potentialDuplicates.Values.ToList();
            result.MatchDetails = matchDetails;
            result.HasDuplicates = potentialDuplicates.Any();

            _logger.LogInformation("Duplicate check for device {Hostname} found {Count} potential duplicates",
                device.Hostname, potentialDuplicates.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate devices for hostname: {Hostname}", device.Hostname);
            return result;
        }
    }

    /// <summary>
    /// Merges new device data with existing device data based on resolution options
    /// </summary>
    public async Task<bool> MergeDeviceDataAsync(Models.Device existingDevice, Models.Device newDevice, Models.DuplicateResolutionOptions resolutionOptions)
    {
        try
        {
            var mergedDevice = CloneDevice(existingDevice); // We'll create this helper method
            var auditReason = $"Device merge: {resolutionOptions.ResolutionReason ?? "Data refresh/update"}";

            // Apply merge logic based on resolution options
            foreach (var fieldToMerge in resolutionOptions.FieldsToMerge)
            {
                switch (fieldToMerge.ToLower())
                {
                    case "hostname":
                        if (!string.IsNullOrWhiteSpace(newDevice.Hostname))
                            mergedDevice.Hostname = newDevice.Hostname;
                        break;
                    case "manufacturer":
                        if (!string.IsNullOrWhiteSpace(newDevice.Manufacturer))
                            mergedDevice.Manufacturer = newDevice.Manufacturer;
                        break;
                    case "model":
                        if (!string.IsNullOrWhiteSpace(newDevice.Model))
                            mergedDevice.Model = newDevice.Model;
                        break;
                    case "serialnumber":
                        if (!string.IsNullOrWhiteSpace(newDevice.SerialNumber))
                            mergedDevice.SerialNumber = newDevice.SerialNumber;
                        break;
                    case "assettag":
                        if (!string.IsNullOrWhiteSpace(newDevice.AssetTag))
                            mergedDevice.AssetTag = newDevice.AssetTag;
                        break;
                    case "cpuinfo":
                        if (!string.IsNullOrWhiteSpace(newDevice.CpuInfo))
                            mergedDevice.CpuInfo = newDevice.CpuInfo;
                        break;
                    case "totalramgb":
                        if (newDevice.TotalRamGb > 0)
                            mergedDevice.TotalRamGb = newDevice.TotalRamGb;
                        break;
                    case "ramtype":
                        if (!string.IsNullOrWhiteSpace(newDevice.RamType))
                            mergedDevice.RamType = newDevice.RamType;
                        break;
                    case "ramspeed":
                        if (!string.IsNullOrWhiteSpace(newDevice.RamSpeed))
                            mergedDevice.RamSpeed = newDevice.RamSpeed;
                        break;
                    case "rammanufacturer":
                        if (!string.IsNullOrWhiteSpace(newDevice.RamManufacturer))
                            mergedDevice.RamManufacturer = newDevice.RamManufacturer;
                        break;
                    case "osname":
                        if (!string.IsNullOrWhiteSpace(newDevice.OsName))
                            mergedDevice.OsName = newDevice.OsName;
                        break;
                    case "osversion":
                        if (!string.IsNullOrWhiteSpace(newDevice.OSVersion))
                            mergedDevice.OSVersion = newDevice.OSVersion;
                        break;
                    case "osarchitecture":
                        if (!string.IsNullOrWhiteSpace(newDevice.OsArchitecture))
                            mergedDevice.OsArchitecture = newDevice.OsArchitecture;
                        break;
                    case "storageinfo":
                        if (!string.IsNullOrWhiteSpace(newDevice.StorageInfo))
                            mergedDevice.StorageInfo = newDevice.StorageInfo;
                        break;
                    case "storagetype":
                        if (!string.IsNullOrWhiteSpace(newDevice.StorageType))
                            mergedDevice.StorageType = newDevice.StorageType;
                        break;
                    case "storagemodel":
                        if (!string.IsNullOrWhiteSpace(newDevice.StorageModel))
                            mergedDevice.StorageModel = newDevice.StorageModel;
                        break;
                    case "primaryip":
                        if (!string.IsNullOrWhiteSpace(newDevice.PrimaryIp))
                            mergedDevice.PrimaryIp = newDevice.PrimaryIp;
                        break;
                    case "primarymac":
                        if (!string.IsNullOrWhiteSpace(newDevice.PrimaryMac))
                            mergedDevice.PrimaryMac = newDevice.PrimaryMac;
                        break;
                    case "primarysubnet":
                        if (!string.IsNullOrWhiteSpace(newDevice.PrimarySubnet))
                            mergedDevice.PrimarySubnet = newDevice.PrimarySubnet;
                        break;
                    case "primarydns":
                        if (!string.IsNullOrWhiteSpace(newDevice.PrimaryDns))
                            mergedDevice.PrimaryDns = newDevice.PrimaryDns;
                        break;
                    case "secondarydns":
                        if (!string.IsNullOrWhiteSpace(newDevice.SecondaryDns))
                            mergedDevice.SecondaryDns = newDevice.SecondaryDns;
                        break;
                    case "all_hardware":
                        // Merge all hardware-related fields
                        if (!string.IsNullOrWhiteSpace(newDevice.Manufacturer)) mergedDevice.Manufacturer = newDevice.Manufacturer;
                        if (!string.IsNullOrWhiteSpace(newDevice.Model)) mergedDevice.Model = newDevice.Model;
                        if (!string.IsNullOrWhiteSpace(newDevice.CpuInfo)) mergedDevice.CpuInfo = newDevice.CpuInfo;
                        if (newDevice.TotalRamGb > 0) mergedDevice.TotalRamGb = newDevice.TotalRamGb;
                        if (!string.IsNullOrWhiteSpace(newDevice.RamType)) mergedDevice.RamType = newDevice.RamType;
                        if (!string.IsNullOrWhiteSpace(newDevice.RamSpeed)) mergedDevice.RamSpeed = newDevice.RamSpeed;
                        if (!string.IsNullOrWhiteSpace(newDevice.RamManufacturer)) mergedDevice.RamManufacturer = newDevice.RamManufacturer;
                        if (!string.IsNullOrWhiteSpace(newDevice.StorageInfo)) mergedDevice.StorageInfo = newDevice.StorageInfo;
                        if (!string.IsNullOrWhiteSpace(newDevice.StorageType)) mergedDevice.StorageType = newDevice.StorageType;
                        if (!string.IsNullOrWhiteSpace(newDevice.StorageModel)) mergedDevice.StorageModel = newDevice.StorageModel;
                        break;
                    case "all_network":
                        // Merge all network-related fields
                        if (!string.IsNullOrWhiteSpace(newDevice.PrimaryIp)) mergedDevice.PrimaryIp = newDevice.PrimaryIp;
                        if (!string.IsNullOrWhiteSpace(newDevice.PrimaryMac)) mergedDevice.PrimaryMac = newDevice.PrimaryMac;
                        if (!string.IsNullOrWhiteSpace(newDevice.PrimarySubnet)) mergedDevice.PrimarySubnet = newDevice.PrimarySubnet;
                        if (!string.IsNullOrWhiteSpace(newDevice.PrimaryDns)) mergedDevice.PrimaryDns = newDevice.PrimaryDns;
                        if (!string.IsNullOrWhiteSpace(newDevice.SecondaryDns)) mergedDevice.SecondaryDns = newDevice.SecondaryDns;
                        break;
                }
            }

            // Update discovery information
            mergedDevice.LastDiscovered = DateTime.Now;
            mergedDevice.DiscoveryMethod = newDevice.DiscoveryMethod ?? "Merge Update";
            mergedDevice.UpdatedAt = DateTime.Now;

            // Update the device in the database with audit trail
            var updateSuccess = await UpdateDeviceAsync(mergedDevice, mergedDevice.DeviceType ?? "Other");
            
            if (updateSuccess)
            {
                // Log the merge operation
                await LogAuditEntryAsync(mergedDevice.device_id, "MERGE", "DEVICE_MERGED",
                    $"Existing device data", $"Merged with new scan data: {string.Join(", ", resolutionOptions.FieldsToMerge)}", auditReason);
                
                _logger.LogInformation("Successfully merged device data for {Hostname} (ID: {DeviceId})", 
                    mergedDevice.Hostname, mergedDevice.device_id);
            }

            return updateSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging device data for existing device ID: {DeviceId}", existingDevice.device_id);
            return false;
        }
    }

    /// <summary>
    /// Helper method to clone a device object
    /// </summary>
    private Models.Device CloneDevice(Models.Device original)
    {
        return new Models.Device
        {
            device_id = original.device_id,
            Hostname = original.Hostname,
            SerialNumber = original.SerialNumber,
            AssetTag = original.AssetTag,
            DeviceType = original.DeviceType,
            EquipmentGroup = original.EquipmentGroup,
            DomainName = original.DomainName,
            IsDomainJoined = original.IsDomainJoined,
            Manufacturer = original.Manufacturer,
            Model = original.Model,
            CpuInfo = original.CpuInfo,
            BiosVersion = original.BiosVersion,
            TotalRamGb = original.TotalRamGb,
            RamType = original.RamType,
            RamSpeed = original.RamSpeed,
            RamManufacturer = original.RamManufacturer,
            OsName = original.OsName,
            OSVersion = original.OSVersion,
            OsArchitecture = original.OsArchitecture,
            OsInstallDate = original.OsInstallDate,
            StorageInfo = original.StorageInfo,
            StorageType = original.StorageType,
            StorageModel = original.StorageModel,
            Drive2Name = original.Drive2Name,
            Drive2Capacity = original.Drive2Capacity,
            Drive2Type = original.Drive2Type,
            Drive2Model = original.Drive2Model,
            Drive3Name = original.Drive3Name,
            Drive3Capacity = original.Drive3Capacity,
            Drive3Type = original.Drive3Type,
            Drive3Model = original.Drive3Model,
            Drive4Name = original.Drive4Name,
            Drive4Capacity = original.Drive4Capacity,
            Drive4Type = original.Drive4Type,
            Drive4Model = original.Drive4Model,
            PrimaryIp = original.PrimaryIp,
            PrimaryMac = original.PrimaryMac,
            PrimarySubnet = original.PrimarySubnet,
            PrimaryDns = original.PrimaryDns,
            SecondaryDns = original.SecondaryDns,
            Nic2Name = original.Nic2Name,
            Nic2Ip = original.Nic2Ip,
            Nic2Mac = original.Nic2Mac,
            Nic2Subnet = original.Nic2Subnet,
            Nic3Name = original.Nic3Name,
            Nic3Ip = original.Nic3Ip,
            Nic3Mac = original.Nic3Mac,
            Nic3Subnet = original.Nic3Subnet,
            Nic4Name = original.Nic4Name,
            Nic4Ip = original.Nic4Ip,
            Nic4Mac = original.Nic4Mac,
            Nic4Subnet = original.Nic4Subnet,
            WebInterfaceUrl = original.WebInterfaceUrl,
            DeviceStatus = original.DeviceStatus,
            Area = original.Area,
            Zone = original.Zone,
            Line = original.Line,
            Pitch = original.Pitch,
            Floor = original.Floor,
            Pillar = original.Pillar,
            AdditionalNotes = original.AdditionalNotes,
            PurchaseDate = original.PurchaseDate,
            ServiceDate = original.ServiceDate,
            WarrantyDate = original.WarrantyDate,
            CreatedAt = original.CreatedAt,
            UpdatedAt = original.UpdatedAt,
            LastDiscovered = original.LastDiscovered,
            DiscoveryMethod = original.DiscoveryMethod
        };
    }

    /// <inheritdoc />
    public async Task<DatabaseTestResult> TestStoredConnectionAsync()
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                return new DatabaseTestResult
                {
                    IsSuccess = false,
                    Message = "❌ No stored database credentials found",
                    TestTimestamp = DateTime.Now
                };
            }

            return await TestConnectionAsync(
                credentials.Server,
                credentials.Database,
                credentials.UseWindowsAuthentication,
                credentials.Username,
                credentials.Password,
                credentials.ConnectionTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing stored database connection");

            return new DatabaseTestResult
            {
                IsSuccess = false,
                Message = $"❌ Error accessing stored credentials: {ex.Message}",
                ErrorDetails = ex.ToString(),
                TestTimestamp = DateTime.Now
            };
        }
    }

    /// <inheritdoc />
    public async Task<DatabaseInfo?> GetDatabaseInfoAsync()
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogWarning("No database credentials available for info query");
                return null;
            }

            var connectionString = BuildConnectionString(
                credentials.Server,
                credentials.Database,
                credentials.UseWindowsAuthentication,
                credentials.Username,
                credentials.Password,
                credentials.ConnectionTimeout);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            return await GetDetailedDatabaseInfoAsync(connection, credentials.Database);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database information");
            return null;
        }
    }

    public async Task<List<Models.Device>> GetCamerasAsync()
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetCamerasAsync");
            return new List<Models.Device>();
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = SQLQueryService.GetAllCamerasQuery;
        using var command = new SqlCommand(query, connection);

        var cameras = new List<Models.Device>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            cameras.Add(new Models.Device
            {
                // Map fields as needed, similar to GetDevicesAsync
                Hostname = GetStringOrEmpty(reader, "hostname"),
                Model = GetString(reader, "model"),
                PrimaryIp = GetString(reader, "primary_ip"),
                Area = GetStringOrEmpty(reader, "area"),
                Zone = GetStringOrEmpty(reader, "zone"),
                Line = GetStringOrEmpty(reader, "line"),
                Column = GetStringOrEmpty(reader, "pillar"),
                Level = GetStringOrEmpty(reader, "floor"),
                WebInterfaceUrl = GetStringOrEmpty(reader, "web_interface_url"),
                // ...other fields...
            });
        }

        return cameras;
    }

    public async Task<List<Models.Device>> GetPrintersAsync()
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetPrintersAsync");
            return new List<Models.Device>();
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = SQLQueryService.GetAllPrintersQuery;
        using var command = new SqlCommand(query, connection);

        var printers = new List<Models.Device>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            printers.Add(new Models.Device
            {
                // Map fields as needed, similar to GetDevicesAsync
                Hostname = GetStringOrEmpty(reader, "hostname"),
                Model = GetString(reader, "model"),
                PrimaryIp = GetString(reader, "primary_ip"),
                Area = GetStringOrEmpty(reader, "area"),
                Zone = GetStringOrEmpty(reader, "zone"),
                Line = GetStringOrEmpty(reader, "line"),
                Column = GetStringOrEmpty(reader, "pillar"),
                Level = GetStringOrEmpty(reader, "floor"),
                WebInterfaceUrl = GetStringOrEmpty(reader, "web_interface_url"),
                // ...other fields...
            });
        }

        return printers;
    }

    public async Task<List<Models.Device>> GetNetopsAsync()
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetNetopsAsync");
            return new List<Models.Device>();
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = SQLQueryService.GetAllNetopsQuery;
        using var command = new SqlCommand(query, connection);

        var netops = new List<Models.Device>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            netops.Add(new Models.Device
            {
                // Map fields as needed, similar to GetDevicesAsync
                Hostname = GetStringOrEmpty(reader, "hostname"),
                Model = GetString(reader, "model"),
                PrimaryIp = GetString(reader, "primary_ip"),
                Area = GetStringOrEmpty(reader, "area"),
                Zone = GetStringOrEmpty(reader, "zone"),
                Line = GetStringOrEmpty(reader, "line"),
                Column = GetStringOrEmpty(reader, "pillar"),
                Level = GetStringOrEmpty(reader, "floor"),
                WebInterfaceUrl = GetStringOrEmpty(reader, "web_interface_url"),
                // ...other fields...
            });
        }

        return netops;
    }

    public async Task<List<Models.Device>> GetNetworkDevicesAsync()
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetNetworkDevicesAsync");
            return new List<Models.Device>();
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = SQLQueryService.GetAllNetworkDevicesQuery;
        using var command = new SqlCommand(query, connection);

        var networkDevices = new List<Models.Device>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            networkDevices.Add(new Models.Device
            {
                // Map fields as needed, similar to GetDevicesAsync
                Hostname = GetStringOrEmpty(reader, "hostname"),
                Model = GetString(reader, "model"),
                PrimaryIp = GetString(reader, "primary_ip"),
                Area = GetStringOrEmpty(reader, "area"),
                Zone = GetStringOrEmpty(reader, "zone"),
                Line = GetStringOrEmpty(reader, "line"),
                Column = GetStringOrEmpty(reader, "pillar"),
                Level = GetStringOrEmpty(reader, "floor"),
                WebInterfaceUrl = GetStringOrEmpty(reader, "web_interface_url"),
                // ...other fields...
            });
        }

        return networkDevices;
    }

    #region Device CRUD Operations

    /// <summary>
    /// Gets all devices from the database with optional device type filtering
    /// </summary>
    public async Task<List<Models.Device>> GetDevicesAsync(string? deviceType = null)
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogWarning("No database credentials available for GetDevices");
                return new List<Models.Device>();
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            _logger.LogInformation("GetDevicesAsync: Connecting to Server='{Server}', Database='{Database}', DeviceType='{DeviceType}'", 
                credentials.Server, credentials.Database, deviceType ?? "All");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Sanitize deviceType input to prevent SQL injection
            var sanitizedDeviceType = InputSanitizer.SanitizeDeviceType(deviceType);

            // Build secure WHERE clause with proper parameterization
            bool useParameter = !string.IsNullOrEmpty(sanitizedDeviceType);
            // If no deviceType specified, return all devices (no WHERE clause)

            var query = $@"
                SELECT *
                FROM dbo.devices
                {(string.IsNullOrEmpty(sanitizedDeviceType) ? "" : "WHERE device_type = @DeviceType")}
                ORDER BY device_id";

            using var command = new SqlCommand(query, connection);
            if (useParameter)
            {
                command.Parameters.AddWithValue("@DeviceType", sanitizedDeviceType);
            }

            var devices = new List<Models.Device>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                devices.Add(new Models.Device
                {
                    device_id = GetInt32(reader, "device_id"),
                    Hostname = GetStringOrEmpty(reader, "hostname"),
                    Area = GetStringOrEmpty(reader, "area"),
                    Zone = GetStringOrEmpty(reader, "zone"),
                    Line = GetStringOrEmpty(reader, "line"),
                    Column = GetStringOrEmpty(reader, "pillar"),
                    Level = GetStringOrEmpty(reader, "floor"),
                    Pitch = GetStringOrEmpty(reader, "pitch"),
                    device_type = GetStringOrEmpty(reader, "device_type"),
                    EquipmentGroup = GetStringOrEmpty(reader, "equipment_group"),
                    Department = GetStringOrEmpty(reader, "department"),
                    Location = GetStringOrEmpty(reader, "location"),
                    PrimaryIp = GetString(reader, "primary_ip"),
                    PrimaryMac = GetString(reader, "primary_mac"),
                    SerialNumber = GetString(reader, "serial_number"),
                    AssetTag = GetString(reader, "asset_tag"),
                    Manufacturer = GetString(reader, "manufacturer"),
                    Model = GetString(reader, "model"),
                    PurchaseDate = GetNullableDateTime(reader, "purchase_date"),
                    ServiceDate = GetNullableDateTime(reader, "service_date"),
                    WarrantyDate = GetNullableDateTime(reader, "warranty_date"),
                    AdditionalNotes = GetString(reader, "additional_notes"),
                    WebLink = GetString(reader, "web_link"),
                    WebLinkName = GetString(reader, "web_link_name"),
                    DomainName = GetString(reader, "domain_name"),
                    Workgroup = GetString(reader, "workgroup"),
                    IsDomainJoined = GetBoolean(reader, "is_domain_joined"),
                    CpuInfo = GetString(reader, "cpu_info"),
                    TotalRamGb = GetDecimal(reader, "total_ram_gb"),
                    RamType = GetString(reader, "ram_type"),
                    StorageInfo = GetString(reader, "storage_info"),
                    BiosVersion = GetString(reader, "bios_version"),
                    OsName = GetString(reader, "os_name"),
                    OSVersion = GetString(reader, "os_version"),
                    OsArchitecture = GetString(reader, "os_architecture"),
                    WebInterfaceUrl = GetString(reader, "web_interface_url"),
                    DeviceStatus = GetStringOrEmpty(reader, "device_status"),
                    LastDiscovered = GetNullableDateTime(reader, "last_discovered"),
                    DiscoveryMethod = GetString(reader, "discovery_method"),
                    CreatedAt = GetNullableDateTime(reader, "created_at"),
                    UpdatedAt = GetNullableDateTime(reader, "updated_at"),
                    // Add any other fields as needed
                });
            }

            _logger.LogInformation("Retrieved {Count} devices from database with filter '{DeviceType}'", devices.Count, deviceType ?? "All");
            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving devices from database");
            return new List<Models.Device>();
        }
    }

    /// <summary>
    /// Adds a new device to the database
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

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Use SqlQueryService for consistent query management
                using var devicesCommand = new SqlCommand(SQLQueryService.AddDeviceQuery, connection, transaction);
                devicesCommand.Parameters.AddWithValue("@hostname", device.Hostname);
                devicesCommand.Parameters.AddWithValue("@serial_number", device.SerialNumber ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@asset_tag", device.AssetTag ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_type", sanitizedDeviceType);
                devicesCommand.Parameters.AddWithValue("@equipment_group", device.EquipmentGroup ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@domain_name", device.DomainName ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@is_domain_joined", device.IsDomainJoined ?? false);
                devicesCommand.Parameters.AddWithValue("@manufacturer", device.Manufacturer ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@model", device.Model ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@cpu_info", device.CpuInfo ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@total_ram_gb", device.TotalRamGb);
                devicesCommand.Parameters.AddWithValue("@ram_type", device.RamType ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@ram_speed", device.RamSpeed ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@ram_manufacturer", device.RamManufacturer ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@storage_info", device.StorageInfo ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@storage_type", device.StorageType ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@storage_model", device.StorageModel ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive2_name", device.Drive2Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive2_capacity", device.Drive2Capacity ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive2_type", device.Drive2Type ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive2_model", device.Drive2Model ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive3_name", device.Drive3Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive3_capacity", device.Drive3Capacity ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive3_type", device.Drive3Type ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive3_model", device.Drive3Model ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive4_name", device.Drive4Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive4_capacity", device.Drive4Capacity ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive4_type", device.Drive4Type ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive4_model", device.Drive4Model ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@bios_version", device.BiosVersion ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_name", device.OsName ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_version", device.OSVersion ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_architecture", device.OsArchitecture ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_install_date", device.OsInstallDate ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_ip", device.PrimaryIp ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_mac", device.PrimaryMac ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_subnet", device.PrimarySubnet ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_dns", device.PrimaryDns ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@secondary_dns", device.SecondaryDns ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic2_name", device.Nic2Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic2_ip", device.Nic2Ip ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic2_mac", device.Nic2Mac ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic2_subnet", device.Nic2Subnet ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic3_name", device.Nic3Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic3_ip", device.Nic3Ip ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic3_mac", device.Nic3Mac ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic3_subnet", device.Nic3Subnet ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic4_name", device.Nic4Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic4_ip", device.Nic4Ip ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic4_mac", device.Nic4Mac ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic4_subnet", device.Nic4Subnet ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@web_interface_url", device.WebInterfaceUrl ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_status", device.device_status ?? device.DeviceStatus ?? "Active");
                devicesCommand.Parameters.AddWithValue("@area", device.Area ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@zone", device.Zone ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@line", device.Line ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pitch", device.Pitch ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@floor", device.Floor ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pillar", device.Pillar ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@additional_notes", device.AdditionalNotes ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@purchase_date", device.PurchaseDate ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@service_date", device.ServiceDate ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@warranty_date", device.WarrantyDate ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@created_at", device.CreatedAt ?? DateTime.Now);
                devicesCommand.Parameters.AddWithValue("@updated_at", device.UpdatedAt ?? DateTime.Now);
                devicesCommand.Parameters.AddWithValue("@last_discovered", device.LastDiscovered ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@discovery_method", device.DiscoveryMethod ?? (object)DBNull.Value);

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

    /// <summary>
    /// Adds a new device to the database with comprehensive audit logging
    /// </summary>
    public async Task<bool> AddDeviceAsync(Models.Device device, string deviceType, string applicationUser, Guid? discoverySessionId, string changeReason)
    {
        try
        {
            // Use the existing AddDeviceAsync method for the main operation
            // The INSERT trigger will automatically handle audit logging
            var success = await AddDeviceAsync(device, deviceType);
            
            if (success && device.device_id > 0)
            {
                // Use application session ID if no specific discovery session provided
                var sessionId = discoverySessionId ?? ApplicationSession.SessionId;
                
                // Update the audit entry created by the trigger with discovery session info
                await UpdateAuditEntryWithSessionInfoAsync(device.device_id, "CREATE", sessionId, applicationUser, changeReason);
                
                _logger.LogInformation("Added device {DeviceName} with automatic audit logging via trigger. Session: {SessionId}, Reason: {Reason}", 
                    device.Hostname, sessionId, changeReason);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding device {DeviceName} with audit logging", device.Hostname);
            return false;
        }
    }

    /// <summary>
    /// Updates an existing device in the database
    /// </summary>
    public async Task<bool> UpdateDeviceAsync(Models.Device device, string deviceType = "Other")
    {
        try
        {
            // Sanitize all input data to prevent SQL injection
            InputSanitizer.SanitizeDevice(device);
            var sanitizedDeviceType = InputSanitizer.SanitizeDeviceType(deviceType);

            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogError("No database credentials available for UpdateDevice");
                return false;
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // First, log that an update operation is starting
                await LogUpdateAuditEntryAsync(connection, transaction, device.device_id, "UPDATE_STARTED", 
                    "Manual device update initiated via application");

                // Use SqlQueryService for consistent query management
                using var devicesCommand = new SqlCommand(SQLQueryService.UpdateDeviceQuery, connection, transaction);
                devicesCommand.Parameters.AddWithValue("@device_id", device.device_id);
                devicesCommand.Parameters.AddWithValue("@hostname", device.Hostname);
                devicesCommand.Parameters.AddWithValue("@serial_number", device.SerialNumber ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@asset_tag", device.AssetTag ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_type", sanitizedDeviceType);
                devicesCommand.Parameters.AddWithValue("@equipment_group", device.EquipmentGroup ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@domain_name", device.DomainName ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@is_domain_joined", device.IsDomainJoined ?? false);
                devicesCommand.Parameters.AddWithValue("@manufacturer", device.Manufacturer ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@model", device.Model ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@cpu_info", device.CpuInfo ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@total_ram_gb", device.TotalRamGb);
                devicesCommand.Parameters.AddWithValue("@ram_type", device.RamType ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@ram_speed", device.RamSpeed ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@ram_manufacturer", device.RamManufacturer ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@storage_info", device.StorageInfo ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@storage_type", device.StorageType ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@storage_model", device.StorageModel ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive2_name", device.Drive2Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive2_capacity", device.Drive2Capacity ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive2_type", device.Drive2Type ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive2_model", device.Drive2Model ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive3_name", device.Drive3Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive3_capacity", device.Drive3Capacity ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive3_type", device.Drive3Type ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive3_model", device.Drive3Model ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive4_name", device.Drive4Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive4_capacity", device.Drive4Capacity ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive4_type", device.Drive4Type ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@drive4_model", device.Drive4Model ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@bios_version", device.BiosVersion ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_name", device.OsName ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_version", device.OSVersion ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_architecture", device.OsArchitecture ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@os_install_date", device.OsInstallDate ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_ip", device.PrimaryIp ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_mac", device.PrimaryMac ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_subnet", device.PrimarySubnet ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@primary_dns", device.PrimaryDns ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@secondary_dns", device.SecondaryDns ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic2_name", device.Nic2Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic2_ip", device.Nic2Ip ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic2_mac", device.Nic2Mac ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic2_subnet", device.Nic2Subnet ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic3_name", device.Nic3Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic3_ip", device.Nic3Ip ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic3_mac", device.Nic3Mac ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic3_subnet", device.Nic3Subnet ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic4_name", device.Nic4Name ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic4_ip", device.Nic4Ip ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic4_mac", device.Nic4Mac ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@nic4_subnet", device.Nic4Subnet ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@web_interface_url", device.WebInterfaceUrl ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_status", device.device_status ?? device.DeviceStatus ?? "Active");
                devicesCommand.Parameters.AddWithValue("@area", device.Area ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@zone", device.Zone ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@line", device.Line ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pitch", device.Pitch ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@floor", device.Floor ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pillar", device.Pillar ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@additional_notes", device.AdditionalNotes ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@purchase_date", device.PurchaseDate ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@service_date", device.ServiceDate ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@warranty_date", device.WarrantyDate ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@updated_at", device.UpdatedAt ?? DateTime.Now);
                devicesCommand.Parameters.AddWithValue("@last_discovered", device.LastDiscovered ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@discovery_method", device.DiscoveryMethod ?? (object)DBNull.Value);

                var devicesRowsAffected = await devicesCommand.ExecuteNonQueryAsync();

                // Log completion of update
                if (devicesRowsAffected > 0)
                {
                    await LogUpdateAuditEntryAsync(connection, transaction, device.device_id, "UPDATE_COMPLETED", 
                        $"Device update completed successfully via application. Updated {devicesRowsAffected} row(s).");
                }

                await transaction.CommitAsync();

                var success = devicesRowsAffected > 0; // devices must exist, device info is optional

                if (success)
                    _logger.LogInformation("Updated device {DeviceName} (ID: {device_id}) in database with audit logging", device.Hostname, device.device_id);
                else
                    _logger.LogWarning("Device {device_id} not found for update", device.device_id);

                return success;
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

    /// <summary>
    /// Updates an existing device in the database with comprehensive audit logging
    /// </summary>
    public async Task<bool> UpdateDeviceAsync(Models.Device device, string deviceType, string applicationUser, Guid? discoverySessionId, string changeReason)
    {
        try
        {
            // Use the existing UpdateDeviceAsync method for the main operation
            // The UPDATE trigger will automatically handle field-by-field audit logging
            var success = await UpdateDeviceAsync(device, deviceType);
            
            if (success && device.device_id > 0)
            {
                _logger.LogInformation("Updated device {DeviceName} with automatic audit logging via trigger. Session: {SessionId}, Reason: {Reason}", 
                    device.Hostname, discoverySessionId, changeReason);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device {DeviceName} with audit logging", device.Hostname);
            return false;
        }
    }

    /// <summary>
    /// Helper method to log audit entries within a transaction
    /// </summary>
    private async Task LogUpdateAuditEntryAsync(SqlConnection connection, SqlTransaction transaction, int deviceId, string actionType, string changeReason)
    {
        try
        {
            const string auditQuery = @"
                EXEC dbo.LogCustomAuditEntry 
                    @DeviceId = @DeviceID,
                    @ActionType = @ActionType,
                    @FieldName = 'APPLICATION_UPDATE',
                    @NewValue = @ChangeReason,
                    @ChangeReason = @ChangeReason";

            using var auditCommand = new SqlCommand(auditQuery, connection, transaction);
            auditCommand.Parameters.AddWithValue("@DeviceID", deviceId);
            auditCommand.Parameters.AddWithValue("@ActionType", actionType);
            auditCommand.Parameters.AddWithValue("@ChangeReason", changeReason);
            await auditCommand.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log audit entry for device {device_id}", deviceId);
            // Don't throw - audit logging failure shouldn't fail the main operation
        }
    }

    /// <summary>
    /// Deletes a device from the database (archives it automatically via trigger)
    /// </summary>
    public async Task<bool> DeleteDeviceAsync(int deviceId)
    {
        return await DeleteDeviceAsync(deviceId, "Device deleted via application");
    }

    /// <summary>
    /// Deletes a device from the database with a specific reason (archives it automatically via trigger)
    /// </summary>
    public async Task<bool> DeleteDeviceAsync(int deviceId, string? deletionReason = null)
    {
        return await DeleteDeviceAsync(deviceId, deletionReason, null, null);
    }

    /// <summary>
    /// Deletes a device from the database with full audit information (archives it automatically via trigger)
    /// </summary>
    public async Task<bool> DeleteDeviceAsync(int deviceId, string? deletionReason = null, Guid? discoverySessionId = null, string? applicationUser = null)
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogError("No database credentials available for DeleteDevice");
                return false;
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Simple DELETE command - INSTEAD OF DELETE trigger handles archiving automatically
            const string deleteQuery = "DELETE FROM dbo.devices WHERE device_id = @DeviceID";
            using var deleteCommand = new SqlCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@DeviceID", deviceId);
            var rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

            // Update the audit log entry created by the trigger with the user-provided deletion reason and session info
            if (rowsAffected > 0)
            {
                try
                {
                    // Use application session ID if no specific discovery session provided
                    var sessionId = discoverySessionId ?? ApplicationSession.SessionId;
                    
                    // Build dynamic update query based on what needs to be updated
                    var updateFields = new List<string>();
                    var updateCommand = new SqlCommand("", connection);
                    updateCommand.Parameters.AddWithValue("@DeviceId", deviceId);
                    updateCommand.Parameters.AddWithValue("@DiscoverySessionId", sessionId);
                    
                    // Always update deletion reason and session ID
                    updateFields.Add("change_reason = @DeletionReason");
                    updateFields.Add("discovery_session_id = @DiscoverySessionId");
                    updateCommand.Parameters.AddWithValue("@DeletionReason", deletionReason ?? "Device deleted via application");
                    
                    // Only update application_user if explicitly provided (let trigger use SUSER_SNAME() otherwise)
                    if (!string.IsNullOrEmpty(applicationUser))
                    {
                        updateFields.Add("application_user = @ApplicationUser");
                        updateCommand.Parameters.AddWithValue("@ApplicationUser", applicationUser);
                    }
                    
                    var updateAuditQuery = $@"
                        UPDATE device_audit_log 
                        SET {string.Join(", ", updateFields)}
                        WHERE device_id = @DeviceId 
                            AND action_type = 'DELETE' 
                            AND field_name = 'DEVICE_DELETED'
                            AND performed_at >= DATEADD(SECOND, -5, GETDATE())"; // Within last 5 seconds

                    updateCommand.CommandText = updateAuditQuery;
                    
                    using (updateCommand)
                    {
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                    
                    _logger.LogInformation("Deleted device {deviceId} with session tracking. Session: {SessionId}", deviceId, sessionId);
                }
                catch (Exception auditEx)
                {
                    // Don't fail the deletion if audit logging fails
                    _logger.LogWarning(auditEx, "Failed to update deletion audit info for device {deviceId}", deviceId);
                }
            }

            var success = rowsAffected > 0;

            if (success)
                _logger.LogInformation("Deleted device {deviceId} from database (archived to deleted_devices via INSTEAD OF DELETE trigger)", deviceId);
            else
                _logger.LogWarning("Device {deviceId} not found for deletion", deviceId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {deviceId} from database", deviceId);
            return false;
        }
    }

    /// <summary>
    /// Archives a device to deleted_devices table using stored procedure
    /// </summary>
    public async Task<bool> ArchiveDeviceAsync(int deviceId, string? deletionReason = null)
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogError("No database credentials available for ArchiveDevice");
                return false;
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string archiveQuery = "EXEC dbo.ArchiveDeletedDevice @DeviceId = @DeviceID, @DeletionReason = @DeletionReason";
            using var command = new SqlCommand(archiveQuery, connection);
            command.Parameters.AddWithValue("@DeviceID", deviceId);
            command.Parameters.AddWithValue("@DeletionReason", deletionReason ?? (object)DBNull.Value);

            var result = await command.ExecuteNonQueryAsync();

            var success = result >= 0; // Stored procedure execution success

            if (success)
                _logger.LogInformation("Archived device {device_id} to deleted_devices table", deviceId);
            else
                _logger.LogWarning("Failed to archive device {device_id}", deviceId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving device {device_id}", deviceId);
            return false;
        }
    }

    /// <summary>
    /// Restores a device from deleted_devices table back to active devices
    /// </summary>
    public async Task<bool> RestoreDeviceAsync(int deletedDeviceId, string? restoreReason = null)
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogError("No database credentials available for RestoreDevice");
                return false;
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string restoreQuery = "EXEC dbo.RestoreDeletedDevice @DeletedDeviceId = @DeletedDeviceID, @RestoreReason = @RestoreReason";
            using var command = new SqlCommand(restoreQuery, connection);
            command.Parameters.AddWithValue("@DeletedDeviceID", deletedDeviceId);
            command.Parameters.AddWithValue("@RestoreReason", restoreReason ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();

            var success = result != null && result != DBNull.Value;

            if (success)
                _logger.LogInformation("Restored device from deleted_devices (deleted_device_id: {deleted_device_id}) back to active devices", deletedDeviceId);
            else
                _logger.LogWarning("Failed to restore device {deleted_device_id}", deletedDeviceId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring device {deleted_device_id}", deletedDeviceId);
            return false;
        }
    }

    /// <summary>
    /// Logs a custom audit entry for manual tracking
    /// </summary>
    public async Task<bool> LogAuditEntryAsync(int deviceId, string actionType, string? fieldName = null, string? oldValue = null, string? newValue = null, string? changeReason = null)
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogError("No database credentials available for LogAuditEntry");
                return false;
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string auditQuery = @"
                EXEC dbo.LogCustomAuditEntry 
                    @DeviceId = @DeviceID,
                    @ActionType = @ActionType,
                    @FieldName = @FieldName,
                    @OldValue = @OldValue,
                    @NewValue = @NewValue,
                    @ChangeReason = @ChangeReason";

            using var command = new SqlCommand(auditQuery, connection);
            command.Parameters.AddWithValue("@DeviceID", deviceId);
            command.Parameters.AddWithValue("@ActionType", actionType);
            command.Parameters.AddWithValue("@FieldName", fieldName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@OldValue", oldValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@NewValue", newValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ChangeReason", changeReason ?? (object)DBNull.Value);

            var result = await command.ExecuteNonQueryAsync();

            var success = result >= 0;

            if (success)
                _logger.LogInformation("Logged audit entry for device {device_id}: {action_type}", deviceId, actionType);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit entry for device {device_id}", deviceId);
            return false;
        }
    }

    #endregion

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

    private static async Task<string> GetServerInfoAsync(SqlConnection connection)
    {
        try
        {
            const string query = "SELECT @@SERVERNAME as ServerName, @@VERSION as Version";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var serverName = reader["ServerName"].ToString() ?? "Unknown";
                var version = reader["Version"].ToString() ?? "Unknown";

                // Extract just the version number for cleaner display
                var versionParts = version.Split('\n')[0].Trim();
                return $"{serverName} - {versionParts}";
            }

            return "SQL Server";
        }
        catch
        {
            return "SQL Server";
        }
    }

    private static async Task<DatabaseInfo> GetDetailedDatabaseInfoAsync(SqlConnection connection, string databaseName)
    {
        const string query = @"
            SELECT 
                DB_NAME() as DatabaseName,
                @@SERVERNAME as ServerName,
                @@VERSION as ServerVersion,
                SERVERPROPERTY('ProductVersion') as ProductVersion,
                SERVERPROPERTY('Edition') as Edition,
                DATABASEPROPERTYEX(DB_NAME(), 'Status') as Status,
                d.create_date as CreatedDate,
                d.collation_name as Collation,
                DATABASEPROPERTYEX(DB_NAME(), 'Collation') as DatabaseCollation,
                SUM(CAST(f.size as bigint) * 8 / 1024) as SizeInMB,
                (SELECT COUNT(*) FROM sys.tables WHERE type = 'U') as UserTables
            FROM sys.database_files f
            CROSS JOIN sys.databases d
            WHERE d.name = DB_NAME()
            GROUP BY d.create_date, d.collation_name";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new DatabaseInfo
            {
                DatabaseName = reader["DatabaseName"].ToString() ?? databaseName,
                ServerName = reader["ServerName"].ToString() ?? "Unknown",
                ServerVersion = reader["ServerVersion"].ToString() ?? "Unknown",
                ProductVersion = reader["ProductVersion"].ToString() ?? "Unknown",
                Edition = reader["Edition"].ToString() ?? "Unknown",
                IsOnline = reader["Status"].ToString() == "ONLINE",
                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.MinValue,
                Collation = reader["DatabaseCollation"].ToString() ?? "Unknown",
                SizeInMB = Convert.ToInt64(reader["SizeInMB"]),
                UserTables = Convert.ToInt32(reader["UserTables"])
            };
        }

        return new DatabaseInfo { DatabaseName = databaseName };
    }

    private static string GetUserFriendlySqlErrorMessage(SqlException sqlEx)
    {
        return sqlEx.Number switch
        {
            2 => "Server not found or not accessible. Check server name and network connectivity.",
            18456 => "Login failed. Check username and password.",
            4060 => "Database not found or access denied. Check database name and permissions.",
            53 => "Network error. Could not establish connection to server.",
            2146893022 => "SSL/Certificate error. Check encryption settings.",
            -2 => "Connection timeout. Server may be busy or unreachable.",
            18452 => "Login failed for user. Check credentials and permissions.",
            233 => "Connection was forcibly closed. Check network and server status.",
            _ => sqlEx.Message
        };
    }

    /// <summary>
    /// Maps a SqlDataReader row to a comprehensive Device object with all enhanced fields
    /// </summary>
    private static Models.Device MapDeviceFromReader(SqlDataReader reader)
    {
        return new Models.Device
        {
            device_id = GetInt32(reader, "device_id"),
            Hostname = GetStringOrEmpty(reader, "hostname"),
            SerialNumber = GetStringOrEmpty(reader, "serial_number"),
            AssetTag = GetStringOrEmpty(reader, "asset_tag"),
            DeviceType = GetStringOrEmpty(reader, "device_type"),
            EquipmentGroup = GetStringOrEmpty(reader, "equipment_group"),
            Department = string.Empty, // Legacy field
            Location = $"{GetStringOrEmpty(reader, "area")} - {GetStringOrEmpty(reader, "zone")} - {GetStringOrEmpty(reader, "line")}",
            
            // Location fields
            Area = GetStringOrEmpty(reader, "area"),
            Zone = GetStringOrEmpty(reader, "zone"),
            Line = GetStringOrEmpty(reader, "line"),
            Column = GetStringOrEmpty(reader, "pillar"),
            Level = GetStringOrEmpty(reader, "floor"),
            Pitch = GetStringOrEmpty(reader, "pitch"),
            Floor = GetStringOrEmpty(reader, "floor"),
            Pillar = GetStringOrEmpty(reader, "pillar"),
            
            // Domain/Workgroup Information
            DomainName = GetString(reader, "domain_name"),
            IsDomainJoined = GetBoolean(reader, "is_domain_joined"),
            
            // Hardware Information
            Manufacturer = GetString(reader, "manufacturer"),
            Model = GetString(reader, "model"),
            CpuInfo = GetString(reader, "cpu_info"),
            BiosVersion = GetString(reader, "bios_version"),
            
            // Memory Information
            TotalRamGb = GetDecimal(reader, "total_ram_gb"),
            RamType = GetString(reader, "ram_type"),
            RamSpeed = GetString(reader, "ram_speed"),
            RamManufacturer = GetString(reader, "ram_manufacturer"),
            
            // Operating System Information
            OsName = GetString(reader, "os_name"),
            OSVersion = GetString(reader, "os_version"),
            OsArchitecture = GetString(reader, "os_architecture"),
            OsInstallDate = GetNullableDateTime(reader, "os_install_date"),
            
            // Primary Storage Information
            StorageInfo = GetString(reader, "storage_info"),
            StorageType = GetString(reader, "storage_type"),
            StorageModel = GetString(reader, "storage_model"),
            
            // Additional Storage Drives
            Drive2Name = GetString(reader, "drive2_name"),
            Drive2Capacity = GetString(reader, "drive2_capacity"),
            Drive2Type = GetString(reader, "drive2_type"),
            Drive2Model = GetString(reader, "drive2_model"),
            
            Drive3Name = GetString(reader, "drive3_name"),
            Drive3Capacity = GetString(reader, "drive3_capacity"),
            Drive3Type = GetString(reader, "drive3_type"),
            Drive3Model = GetString(reader, "drive3_model"),
            
            Drive4Name = GetString(reader, "drive4_name"),
            Drive4Capacity = GetString(reader, "drive4_capacity"),
            Drive4Type = GetString(reader, "drive4_type"),
            Drive4Model = GetString(reader, "drive4_model"),
            
            // Primary Network Interface
            PrimaryIp = GetString(reader, "primary_ip"),
            PrimaryMac = GetString(reader, "primary_mac"),
            PrimarySubnet = GetString(reader, "primary_subnet"),
            PrimaryDns = GetString(reader, "primary_dns"),
            SecondaryDns = GetString(reader, "secondary_dns"),
            
            // Network Interface 2
            Nic2Name = GetString(reader, "nic2_name"),
            Nic2Ip = GetString(reader, "nic2_ip"),
            Nic2Mac = GetString(reader, "nic2_mac"),
            Nic2Subnet = GetString(reader, "nic2_subnet"),
            
            // Network Interface 3
            Nic3Name = GetString(reader, "nic3_name"),
            Nic3Ip = GetString(reader, "nic3_ip"),
            Nic3Mac = GetString(reader, "nic3_mac"),
            Nic3Subnet = GetString(reader, "nic3_subnet"),
            
            // Network Interface 4
            Nic4Name = GetString(reader, "nic4_name"),
            Nic4Ip = GetString(reader, "nic4_ip"),
            Nic4Mac = GetString(reader, "nic4_mac"),
            Nic4Subnet = GetString(reader, "nic4_subnet"),
            
            // Web Interface
            WebInterfaceUrl = GetString(reader, "web_interface_url"),
            
            // Device Status and Additional Information
            DeviceStatus = GetStringOrEmpty(reader, "device_status"),
            AdditionalNotes = GetString(reader, "additional_notes"),
            
            // Asset Management Dates
            PurchaseDate = GetNullableDateTime(reader, "purchase_date"),
            ServiceDate = GetNullableDateTime(reader, "service_date"),
            WarrantyDate = GetNullableDateTime(reader, "warranty_date"),
            
            // System Fields
            CreatedAt = GetNullableDateTime(reader, "created_at"),
            UpdatedAt = GetNullableDateTime(reader, "updated_at"),
            LastDiscovered = GetNullableDateTime(reader, "last_discovered"),
            DiscoveryMethod = GetString(reader, "discovery_method"),
            
            // Legacy fields for compatibility
            WebLink = GetString(reader, "web_interface_url"), // Map to WebInterfaceUrl
            WebLinkName = GetString(reader, "hostname"), // Use hostname as default web link name
        };
    }

    private static string GetStringOrEmpty(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        try
        {
            reader.GetOrdinal(columnName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int GetInt32OrDefault(SqlDataReader reader, string columnName, int defaultValue)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt32(ordinal);
        }
        catch
        {
            return defaultValue;
        }
    }

    private static bool GetBooleanOrDefault(SqlDataReader reader, string columnName, bool defaultValue)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetBoolean(ordinal);
        }
        catch
        {
            return defaultValue;
        }
    }

    private static decimal GetDecimalOrDefault(SqlDataReader reader, string columnName, decimal defaultValue)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDecimal(ordinal);
        }
        catch
        {
            return defaultValue;
        }
    }

    private static string? GetString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int GetInt32(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
    }

    private static bool GetBoolean(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? false : reader.GetBoolean(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        // Handle both DateTime and string date formats since our DB stores dates as nvarchar
        var value = reader.GetValue(ordinal);
        if (value is DateTime dateTime)
            return dateTime;

        if (value is string dateString && !string.IsNullOrWhiteSpace(dateString))
        {
            if (DateTime.TryParse(dateString, out var parsedDate))
                return parsedDate;
        }

        return null;
    }
    
    public async Task<List<Models.Device>> SearchDevicesAsync(string searchQuery)
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for SearchDevicesAsync");
            return new List<Models.Device>();
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        _logger.LogInformation("SearchDevicesAsync: Connecting to Server='{Server}', Database='{Database}', Query='{Query}'", 
            credentials.Server, credentials.Database, searchQuery);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Use SqlQueryService for consistent query management
        using var command = new SqlCommand(SQLQueryService.SearchDevicesQuery, connection);
        command.Parameters.AddWithValue("@searchTerm", "%" + searchQuery + "%");

        var results = new List<Models.Device>();
        using var reader = await command.ExecuteReaderAsync();
        
        _logger.LogInformation("SearchDevicesAsync: SqlDataReader created, checking for results...");
        
        int recordCount = 0;
        while (await reader.ReadAsync())
        {
            recordCount++;
            _logger.LogInformation("SearchDevicesAsync: Processing record {RecordNumber}", recordCount);
            
            try
            {
                var device = new Models.Device
                {
                    // Essential fields that always exist
                    device_id = GetInt32OrDefault(reader, "device_id", 0),
                    Hostname = GetStringOrEmpty(reader, "hostname"),
                    
                    // Basic device info
                    SerialNumber = GetStringOrEmpty(reader, "serial_number"),
                    AssetTag = GetStringOrEmpty(reader, "asset_tag"),
                    DomainName = GetStringOrEmpty(reader, "domain_name"),
                    IsDomainJoined = GetBooleanOrDefault(reader, "is_domain_joined", false),
                    
                    // Hardware
                    Manufacturer = GetStringOrEmpty(reader, "manufacturer"),
                    Model = GetStringOrEmpty(reader, "model"),
                    CpuInfo = GetStringOrEmpty(reader, "cpu_info"),
                    BiosVersion = GetStringOrEmpty(reader, "bios_version"),
                    
                    // Memory
                    TotalRamGb = GetDecimalOrDefault(reader, "total_ram_gb", 0),
                    RamType = GetStringOrEmpty(reader, "ram_type"),
                    RamSpeed = GetStringOrEmpty(reader, "ram_speed"),
                    RamManufacturer = GetStringOrEmpty(reader, "ram_manufacturer"),
                    
                    // OS
                    OsName = GetStringOrEmpty(reader, "os_name"),
                    OSVersion = GetStringOrEmpty(reader, "os_version"),
                    OsArchitecture = GetStringOrEmpty(reader, "os_architecture"),
                    OsInstallDate = GetNullableDateTime(reader, "os_install_date"),
                    
                    // Storage
                    StorageInfo = GetStringOrEmpty(reader, "storage_info"),
                    StorageType = GetStringOrEmpty(reader, "storage_type"),
                    StorageModel = GetStringOrEmpty(reader, "storage_model"),
                    
                    // Additional Storage Drives
                    Drive2Name = GetStringOrEmpty(reader, "drive2_name"),
                    Drive2Capacity = GetStringOrEmpty(reader, "drive2_capacity"),
                    Drive2Type = GetStringOrEmpty(reader, "drive2_type"),
                    Drive2Model = GetStringOrEmpty(reader, "drive2_model"),
                    
                    Drive3Name = GetStringOrEmpty(reader, "drive3_name"),
                    Drive3Capacity = GetStringOrEmpty(reader, "drive3_capacity"),
                    Drive3Type = GetStringOrEmpty(reader, "drive3_type"),
                    Drive3Model = GetStringOrEmpty(reader, "drive3_model"),
                    
                    Drive4Name = GetStringOrEmpty(reader, "drive4_name"),
                    Drive4Capacity = GetStringOrEmpty(reader, "drive4_capacity"),
                    Drive4Type = GetStringOrEmpty(reader, "drive4_type"),
                    Drive4Model = GetStringOrEmpty(reader, "drive4_model"),
                    
                    // Primary Network Interface
                    PrimaryIp = GetStringOrEmpty(reader, "primary_ip"),
                    PrimaryMac = GetStringOrEmpty(reader, "primary_mac"),
                    PrimarySubnet = GetStringOrEmpty(reader, "primary_subnet"),
                    PrimaryDns = GetStringOrEmpty(reader, "primary_dns"),
                    SecondaryDns = GetStringOrEmpty(reader, "secondary_dns"),
                    
                    // Network Interface 2
                    Nic2Name = GetStringOrEmpty(reader, "nic2_name"),
                    Nic2Ip = GetStringOrEmpty(reader, "nic2_ip"),
                    Nic2Mac = GetStringOrEmpty(reader, "nic2_mac"),
                    Nic2Subnet = GetStringOrEmpty(reader, "nic2_subnet"),
                    
                    // Network Interface 3
                    Nic3Name = GetStringOrEmpty(reader, "nic3_name"),
                    Nic3Ip = GetStringOrEmpty(reader, "nic3_ip"),
                    Nic3Mac = GetStringOrEmpty(reader, "nic3_mac"),
                    Nic3Subnet = GetStringOrEmpty(reader, "nic3_subnet"),
                    
                    // Network Interface 4
                    Nic4Name = GetStringOrEmpty(reader, "nic4_name"),
                    Nic4Ip = GetStringOrEmpty(reader, "nic4_ip"),
                    Nic4Mac = GetStringOrEmpty(reader, "nic4_mac"),
                    Nic4Subnet = GetStringOrEmpty(reader, "nic4_subnet"),
                    
                    // Web Interface
                    WebInterfaceUrl = GetStringOrEmpty(reader, "web_interface_url"),
                    
                    // Device classification
                    device_type = GetStringOrEmpty(reader, "device_type"),
                    DeviceStatus = GetStringOrEmpty(reader, "device_status"),
                    EquipmentGroup = GetStringOrEmpty(reader, "equipment_group"),
                    
                    // Location
                    Area = GetStringOrEmpty(reader, "area"),
                    Zone = GetStringOrEmpty(reader, "zone"),
                    Line = GetStringOrEmpty(reader, "line"),
                    Pitch = GetStringOrEmpty(reader, "pitch"),
                    Floor = GetStringOrEmpty(reader, "floor"),
                    Pillar = GetStringOrEmpty(reader, "pillar"),
                    
                    // Additional Information
                    AdditionalNotes = GetStringOrEmpty(reader, "additional_notes"),
                    
                    // Asset Management Dates
                    PurchaseDate = GetNullableDateTime(reader, "purchase_date"),
                    ServiceDate = GetNullableDateTime(reader, "service_date"),
                    WarrantyDate = GetNullableDateTime(reader, "warranty_date"),
                    
                    // System Fields
                    CreatedAt = GetNullableDateTime(reader, "created_at"),
                    UpdatedAt = GetNullableDateTime(reader, "updated_at"),
                    LastDiscovered = GetNullableDateTime(reader, "last_discovered"),
                    DiscoveryMethod = GetStringOrEmpty(reader, "discovery_method")
                };

                _logger.LogInformation("SearchDevicesAsync: Successfully mapped record {RecordNumber} - Hostname: {Hostname}, IP: {IP}", 
                    recordCount, device.Hostname, device.PrimaryIp);
                
                results.Add(device);
            }
            catch (Exception fieldEx)
            {
                _logger.LogError(fieldEx, "SearchDevicesAsync: Error reading device record {RecordNumber} - skipping record. Error: {Error}", 
                    recordCount, fieldEx.Message);
                continue; // Skip this record and continue with the next one
            }
        }

        _logger.LogInformation("SearchDevicesAsync: Processed {RecordCount} total records, returning {ResultCount} devices for query '{Query}'", 
            recordCount, results.Count, searchQuery);
        
        return results;
    }

    private static decimal GetDecimal(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
    }

    /// <summary>
    /// Logs a custom audit entry using the LogCustomAuditEntry stored procedure
    /// </summary>
    private async Task LogCustomAuditEntryAsync(int deviceId, string actionType, string fieldName, string oldValue, string newValue, string applicationUser, string changeReason, Guid? discoverySessionId)
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogWarning("No database credentials available for audit logging");
                return;
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password, credentials.ConnectionTimeout);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("LogCustomAuditEntry", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters for the stored procedure
            command.Parameters.AddWithValue("@DeviceId", deviceId);
            command.Parameters.AddWithValue("@ActionType", actionType);
            command.Parameters.AddWithValue("@FieldName", fieldName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@OldValue", oldValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@NewValue", newValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ApplicationUser", applicationUser ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ChangeReason", changeReason ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DiscoverySessionId", discoverySessionId ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
            
            _logger.LogDebug("Audit entry logged for device {DeviceId}, action {ActionType}, session {SessionId}", 
                deviceId, actionType, discoverySessionId);
        }
        catch (Exception ex)
        {
            // Don't throw - audit logging failure shouldn't fail the main operation
            _logger.LogWarning(ex, "Failed to log audit entry for device {DeviceId}, action {ActionType}", 
                deviceId, actionType);
        }
    }

    /// <summary>
    /// Updates the most recent audit entry for a device with discovery session info and enhanced details
    /// </summary>
    private async Task UpdateAuditEntryWithSessionInfoAsync(int deviceId, string actionType, Guid? discoverySessionId, string applicationUser, string changeReason)
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogWarning("No database credentials available for updating audit entry session info");
                return;
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Use application session ID if no specific session provided
            var sessionId = discoverySessionId ?? ApplicationSession.SessionId;

            // Build dynamic update query - only update application_user if explicitly provided
            var updateFields = new List<string>
            {
                "discovery_session_id = @DiscoverySessionId",
                "change_reason = @ChangeReason"
            };
            
            var command = new SqlCommand("", connection);
            command.Parameters.AddWithValue("@DeviceId", deviceId);
            command.Parameters.AddWithValue("@ActionType", actionType);
            command.Parameters.AddWithValue("@DiscoverySessionId", sessionId);
            command.Parameters.AddWithValue("@ChangeReason", changeReason ?? $"Device {actionType.ToLower()} operation");
            
            // Only update application_user if explicitly provided (let trigger use SUSER_SNAME() otherwise)
            if (!string.IsNullOrEmpty(applicationUser))
            {
                updateFields.Add("application_user = @ApplicationUser");
                command.Parameters.AddWithValue("@ApplicationUser", applicationUser);
            }

            var updateQuery = $@"
                UPDATE device_audit_log 
                SET {string.Join(", ", updateFields)}
                WHERE device_id = @DeviceId 
                    AND action_type = @ActionType
                    AND performed_at >= DATEADD(SECOND, -10, GETDATE())
                    AND discovery_session_id IS NULL";

            command.CommandText = updateQuery;
            
            using (command)
            {
                var rowsUpdated = await command.ExecuteNonQueryAsync();
                
                if (rowsUpdated > 0)
                {
                    _logger.LogDebug("Updated audit entry with session info for device {DeviceId}, action {ActionType}, session {SessionId}", 
                        deviceId, actionType, sessionId);
                }
            }
        }
        catch (Exception ex)
        {
            // Don't throw - audit logging failure shouldn't fail the main operation
            _logger.LogWarning(ex, "Failed to update audit entry session info for device {DeviceId}, action {ActionType}", 
                deviceId, actionType);
        }
    }
}
