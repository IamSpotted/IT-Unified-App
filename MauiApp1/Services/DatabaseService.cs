using System.Diagnostics;
using System.Data.SqlClient;
using MauiApp1.Interfaces;
using MauiApp1.Models;
using DeviceModel = MauiApp1.Models.Device;

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
    public async Task<DeviceModel?> GetDeviceByHostnameAsync(string hostname)
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

        const string query = @"
            SELECT TOP 1 * FROM devices WHERE hostname = @hostname";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@hostname", hostname);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new DeviceModel
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

    public async Task<List<DeviceModel>> GetCamerasAsync()
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetCamerasAsync");
            return new List<DeviceModel>();
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = SQLQueryService.GetAllCamerasQuery;
        using var command = new SqlCommand(query, connection);

        var cameras = new List<DeviceModel>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            cameras.Add(new DeviceModel
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

    public async Task<List<DeviceModel>> GetPrintersAsync()
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetPrintersAsync");
            return new List<DeviceModel>();
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = SQLQueryService.GetAllPrintersQuery;
        using var command = new SqlCommand(query, connection);

        var printers = new List<DeviceModel>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            printers.Add(new DeviceModel
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

    public async Task<List<DeviceModel>> GetNetopsAsync()
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for GetNetopsAsync");
            return new List<DeviceModel>();
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = SQLQueryService.GetAllNetopsQuery;
        using var command = new SqlCommand(query, connection);

        var netops = new List<DeviceModel>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            netops.Add(new DeviceModel
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

    #region Device CRUD Operations

    /// <summary>
    /// Gets all devices from the database with optional device type filtering
    /// </summary>
    public async Task<List<DeviceModel>> GetDevicesAsync(string? deviceType = null)
    {
        try
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogWarning("No database credentials available for GetDevices");
                return new List<DeviceModel>();
            }

            var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
                credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

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

            var devices = new List<DeviceModel>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                devices.Add(new DeviceModel
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
                    SecondaryIps = GetString(reader, "secondary_ips"),
                    SecondaryMacs = GetString(reader, "secondary_macs"),
                    WebInterfaceUrl = GetString(reader, "web_interface_url"),
                    DnsServers = GetString(reader, "dns_servers"),
                    DefaultGateways = GetString(reader, "default_gateways"),
                    SubnetMasks = GetString(reader, "subnet_masks"),
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
            return new List<DeviceModel>();
        }
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

            try
            {
                // Insert into devices table (all columns)
                const string devicesQuery = @"
                    INSERT INTO dbo.devices (
                        hostname, serial_number, asset_tag, device_type, equipment_group,
                        domain_name, workgroup, is_domain_joined, manufacturer, model, cpu_info, total_ram_gb, ram_type, storage_info, bios_version,
                        os_name, os_version, os_architecture, primary_ip, primary_mac, secondary_ips, secondary_macs, web_interface_url,
                        dns_servers, default_gateways, subnet_masks, device_status, area, zone, line, pitch, floor, pillar,
                        additional_notes, created_at, updated_at, last_discovered, discovery_method 
                    ) VALUES (
                        @hostname, @serial_number, @asset_tag, @device_type, @equipment_group,
                        @domain_name, @workgroup, @is_domain_joined, @manufacturer, @model, @cpu_info, @total_ram_gb, @ram_type, @storage_info, @bios_version,
                        @os_name, @os_version, @os_architecture, @primary_ip, @primary_mac, @secondary_ips, @secondary_macs, @web_interface_url,
                        @dns_servers, @default_gateways, @subnet_masks, @device_status, @area, @zone, @line, @pitch, @floor, @pillar,
                        @additional_notes, @created_at, @updated_at, @last_discovered, @discovery_method
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                using var devicesCommand = new SqlCommand(devicesQuery, connection, transaction);
                devicesCommand.Parameters.AddWithValue("@hostname", device.Hostname);
                devicesCommand.Parameters.AddWithValue("@serial_number", device.SerialNumber ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@asset_tag", device.AssetTag ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_type", sanitizedDeviceType);
                devicesCommand.Parameters.AddWithValue("@equipment_group", device.EquipmentGroup ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@domain_name", device.DomainName ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@workgroup", device.Workgroup ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@is_domain_joined", device.IsDomainJoined ?? false);
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
                devicesCommand.Parameters.AddWithValue("@web_interface_url", device.WebInterfaceUrl ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@dns_servers", device.DnsServers ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@default_gateways", device.DefaultGateways ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@subnet_masks", device.SubnetMasks ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_status", device.device_status ?? device.DeviceStatus ?? "Active");
                devicesCommand.Parameters.AddWithValue("@area", device.Area ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@zone", device.Zone ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@line", device.Line ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pitch", device.Pitch ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@floor", device.Floor ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pillar", device.Pillar ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@additional_notes", device.AdditionalNotes ?? (object)DBNull.Value);
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
    /// Updates an existing device in the database
    /// </summary>
    public async Task<bool> UpdateDeviceAsync(DeviceModel device, string deviceType = "Other")
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
                // Update devices table with all columns
                const string devicesQuery = @"
                    UPDATE dbo.devices SET
                        hostname = @hostname,
                        serial_number = @serial_number,
                        asset_tag = @asset_tag,
                        device_type = @device_type,
                        equipment_group = @equipment_group,
                        domain_name = @domain_name,
                        workgroup = @workgroup,
                        is_domain_joined = @is_domain_joined,
                        manufacturer = @manufacturer,
                        model = @model,
                        cpu_info = @cpu_info,
                        total_ram_gb = @total_ram_gb,
                        ram_type = @ram_type,
                        storage_info = @storage_info,
                        bios_version = @bios_version,
                        os_name = @os_name,
                        os_version = @os_version,
                        os_architecture = @os_architecture,
                        primary_ip = @primary_ip,
                        primary_mac = @primary_mac,
                        secondary_ips = @secondary_ips,
                        secondary_macs = @secondary_macs,
                        web_interface_url = @web_interface_url,
                        dns_servers = @dns_servers,
                        default_gateways = @default_gateways,
                        subnet_masks = @subnet_masks,
                        device_status = @device_status,
                        area = @area,
                        zone = @zone,
                        line = @line,
                        pitch = @pitch,
                        floor = @floor,
                        pillar = @pillar,
                        additional_notes = @additional_notes,
                        updated_at = @updated_at,
                        last_discovered = @last_discovered,
                        discovery_method = @discovery_method,
                    WHERE device_id = @device_id";

                using var devicesCommand = new SqlCommand(devicesQuery, connection, transaction);
                devicesCommand.Parameters.AddWithValue("@device_id", device.device_id);
                devicesCommand.Parameters.AddWithValue("@hostname", device.Hostname);
                devicesCommand.Parameters.AddWithValue("@serial_number", device.SerialNumber ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@asset_tag", device.AssetTag ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_type", sanitizedDeviceType);
                devicesCommand.Parameters.AddWithValue("@equipment_group", device.EquipmentGroup ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@domain_name", device.DomainName ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@workgroup", device.Workgroup ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@is_domain_joined", device.IsDomainJoined ?? false);
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
                devicesCommand.Parameters.AddWithValue("@web_interface_url", device.WebInterfaceUrl ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@dns_servers", device.DnsServers ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@default_gateways", device.DefaultGateways ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@subnet_masks", device.SubnetMasks ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@device_status", device.device_status ?? device.DeviceStatus ?? "Active");
                devicesCommand.Parameters.AddWithValue("@area", device.Area ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@zone", device.Zone ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@line", device.Line ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pitch", device.Pitch ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@floor", device.Floor ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@pillar", device.Pillar ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@additional_notes", device.AdditionalNotes ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@updated_at", device.UpdatedAt ?? DateTime.Now);
                devicesCommand.Parameters.AddWithValue("@last_discovered", device.LastDiscovered ?? (object)DBNull.Value);
                devicesCommand.Parameters.AddWithValue("@discovery_method", device.DiscoveryMethod ?? (object)DBNull.Value);

                var devicesRowsAffected = await devicesCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                

                var success = devicesRowsAffected > 0; // devices must exist, device info is optional

                if (success)
                    _logger.LogInformation("Updated device {DeviceName} (ID: {device_id}) in database", device.Hostname, device.device_id);
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
    /// Deletes a device from the database
    /// </summary>
    public async Task<bool> DeleteDeviceAsync(int deviceId)
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

            using var transaction = connection.BeginTransaction();

            try
            {
                // Delete from devices table only
                const string devicesQuery = "DELETE FROM dbo.devices WHERE device_id = @DeviceID";
                using var devicesCommand = new SqlCommand(devicesQuery, connection, transaction);
                devicesCommand.Parameters.AddWithValue("@DeviceID", deviceId);
                var rowsAffected = await devicesCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                var success = rowsAffected > 0;

                if (success)
                    _logger.LogInformation("Deleted device {device_id} from database", deviceId);
                else
                    _logger.LogWarning("Device {device_id} not found for deletion", deviceId);

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
            _logger.LogError(ex, "Error deleting device {device_id} from database", deviceId);
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

    private static string GetStringOrEmpty(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
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
    
    public async Task<List<DeviceModel>> SearchDevicesAsync(string searchQuery)
    {
        var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
        if (credentials == null)
        {
            _logger.LogWarning("No database credentials available for SearchDevicesAsync");
            return new List<DeviceModel>();
        }

        var connectionString = BuildConnectionString(credentials.Server, credentials.Database,
            credentials.UseWindowsAuthentication, credentials.Username, credentials.Password);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT * FROM devices
            WHERE 
                hostname LIKE @query OR
                primary_ip LIKE @query OR
                area LIKE @query OR
                zone LIKE @query OR
                line LIKE @query OR
                serial_number LIKE @query OR
                asset_tag LIKE @query
        ";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@query", "%" + searchQuery + "%");

        var results = new List<DeviceModel>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new DeviceModel
            {
                // Map fields as needed, for example:
                Hostname = GetStringOrEmpty(reader, "hostname"),
                SerialNumber = GetString(reader, "serial_number"),
                AssetTag = GetString(reader, "asset_tag"),
                DomainName = GetString(reader, "domain_name"),
                Workgroup = GetString(reader, "workgroup"),
                IsDomainJoined = GetBoolean(reader, "is_domain_joined"),
                PrimaryIp = GetString(reader, "primary_ip"),
                PrimaryMac = GetString(reader, "primary_mac"),
                SecondaryIps = GetString(reader, "secondary_ips"),
                Manufacturer = GetString(reader, "manufacturer"),
                Model = GetString(reader, "model"),
                CpuInfo = GetString(reader, "cpu_info"),
                TotalRamGb = GetDecimal(reader, "total_ram_gb"),
                RamType = GetString(reader, "ram_type"),
                StorageInfo = GetString(reader, "storage_info"),
                BiosVersion = GetString(reader, "bios_version"),
                OsName = GetString(reader, "os_name"),
                OSVersion = GetString(reader, "os_version"),
                OsArchitecture = GetString(reader, "os_architecture"),
                SecondaryMacs = GetString(reader, "secondary_macs"),
                DiscoveryMethod = GetString(reader, "discovery_method"),
                CreatedAt = GetNullableDateTime(reader, "created_at"),
                UpdatedAt = GetNullableDateTime(reader, "updated_at"),
                LastDiscovered = GetNullableDateTime(reader, "last_discovered"),
                device_type = GetStringOrEmpty(reader, "device_type"),
                SubnetMasks = GetString(reader, "subnet_masks"),
                DeviceStatus = GetStringOrEmpty(reader, "device_status"),
                WebInterfaceUrl = GetStringOrEmpty(reader, "web_interface_url"),
                Area = GetStringOrEmpty(reader, "area"),
                Zone = GetStringOrEmpty(reader, "zone"),
                Line = GetStringOrEmpty(reader, "line"),
                Pitch = GetStringOrEmpty(reader, "pitch"),
                Floor = GetStringOrEmpty(reader, "floor"),
                Pillar = GetStringOrEmpty(reader, "pillar"),
                EquipmentGroup = GetStringOrEmpty(reader, "equipment_group"),
            });
        }

        return results;
    }

    private static decimal GetDecimal(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
    }
}
