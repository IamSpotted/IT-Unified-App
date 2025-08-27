using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Diagnostics;
using MauiApp1.Models;
using MauiApp1.Services;
using MauiApp1.Interfaces;
using DeviceModel = MauiApp1.Models.Device;

namespace MauiApp1.Services
{
    public class GetDevices : IGetDevicesService
    {
        private readonly SecureCredentialsService _credentialsService;
        private readonly ILogger<GetDevices> _logger;

        public GetDevices(SecureCredentialsService credentialsService, ILogger<GetDevices> logger)
        {
            _credentialsService = credentialsService;
            _logger = logger;
        }

        public async Task<List<DeviceModel>> GetAllDevicesAsync()
        {
            var devices = new List<DeviceModel>();

            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
           
            if (credentials == null)
            {
                _logger.LogError("No database credentials available for GetDevices");
                return devices;
            }
            var connectionString = new SqlConnectionStringBuilder
            {
                DataSource = credentials.Server,
                InitialCatalog = credentials.Database,
                UserID = credentials.Username,
                Password = credentials.Password,
                IntegratedSecurity = false
            };

            if (!credentials.UseWindowsAuthentication)
            {
                connectionString.UserID = credentials.Username ?? string.Empty;
                connectionString.Password = credentials.Password ?? string.Empty;
            }

            using var connection = new SqlConnection(connectionString.ConnectionString);
            await connection.OpenAsync();

            var query = SQLQueryService.GetAllDevicesQuery;
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var device = new DeviceModel
                {
                    Hostname = reader.GetString(reader.GetOrdinal("hostname")),
                    SerialNumber = reader.IsDBNull(reader.GetOrdinal("serial_number")) ? null : reader.GetString(reader.GetOrdinal("serial_number")),
                    AssetTag = reader.IsDBNull(reader.GetOrdinal("asset_tag")) ? null : reader.GetString(reader.GetOrdinal("asset_tag")),
                    DomainName = reader.IsDBNull(reader.GetOrdinal("domain_name")) ? null : reader.GetString(reader.GetOrdinal("domain_name")),
                    Workgroup = reader.IsDBNull(reader.GetOrdinal("workgroup")) ? null : reader.GetString(reader.GetOrdinal("workgroup")),
                    IsDomainJoined = reader.GetBoolean(reader.GetOrdinal("is_domain_joined")),
                    Manufacturer = reader.IsDBNull(reader.GetOrdinal("manufacturer")) ? null : reader.GetString(reader.GetOrdinal("manufacturer")),
                    Model = reader.IsDBNull(reader.GetOrdinal("model")) ? null : reader.GetString(reader.GetOrdinal("model")),
                    CpuInfo = reader.IsDBNull(reader.GetOrdinal("cpu_info")) ? null : reader.GetString(reader.GetOrdinal("cpu_info")),
                    TotalRamGb = reader.GetInt32(reader.GetOrdinal("total_ram_gb")),
                    RamType = reader.IsDBNull(reader.GetOrdinal("ram_type")) ? null : reader.GetString(reader.GetOrdinal("ram_type")),
                    StorageInfo = reader.IsDBNull(reader.GetOrdinal("storage_info")) ? null : reader.GetString(reader.GetOrdinal("storage_info")),
                    BiosVersion = reader.IsDBNull(reader.GetOrdinal("bios_version")) ? null : reader.GetString(reader.GetOrdinal("bios_version")),
                    OsName = reader.IsDBNull(reader.GetOrdinal("os_name")) ? null : reader.GetString(reader.GetOrdinal("os_name")),
                    OSVersion = reader.IsDBNull(reader.GetOrdinal("os_version")) ? null : reader.GetString(reader.GetOrdinal("os_version")),
                    OsArchitecture = reader.IsDBNull(reader.GetOrdinal("os_architecture")) ? null : reader.GetString(reader.GetOrdinal("os_architecture")),
                    PrimaryIp = reader.IsDBNull(reader.GetOrdinal("primary_ip")) ? null : reader.GetString(reader.GetOrdinal("primary_ip")),
                    PrimaryMac = reader.IsDBNull(reader.GetOrdinal("primary_mac")) ? null : reader.GetString(reader.GetOrdinal("primary_mac")),
                    SecondaryIps = reader.IsDBNull(reader.GetOrdinal("secondary_ips")) ? null : reader.GetString(reader.GetOrdinal("secondary_ips")),
                    SecondaryMacs = reader.IsDBNull(reader.GetOrdinal("secondary_macs")) ? null : reader.GetString(reader.GetOrdinal("secondary_macs")),
                    DnsServers = reader.IsDBNull(reader.GetOrdinal("dns_servers")) ? null : reader.GetString(reader.GetOrdinal("dns_servers")),
                    DefaultGateways = reader.IsDBNull(reader.GetOrdinal("default_gateways")) ? null : reader.GetString(reader.GetOrdinal("default_gateways")),
                    SubnetMasks = reader.IsDBNull(reader.GetOrdinal("subnet_masks")) ? null : reader.GetString(reader.GetOrdinal("subnet_masks")),
                    Area = reader.IsDBNull(reader.GetOrdinal("area")) ? string.Empty : reader.GetString(reader.GetOrdinal("area")),
                    Zone = reader.IsDBNull(reader.GetOrdinal("zone")) ? string.Empty : reader.GetString(reader.GetOrdinal("zone")),
                    Line = reader.IsDBNull(reader.GetOrdinal("line")) ? string.Empty : reader.GetString(reader.GetOrdinal("line")),
                    Pitch = reader.IsDBNull(reader.GetOrdinal("pitch")) ? string.Empty : reader.GetString(reader.GetOrdinal("pitch")),
                    Floor = reader.IsDBNull(reader.GetOrdinal("floor")) ? string.Empty : reader.GetString(reader.GetOrdinal("floor")),
                    Pillar = reader.IsDBNull(reader.GetOrdinal("pillar")) ? string.Empty : reader.GetString(reader.GetOrdinal("pillar")),
                    AdditionalNotes = reader.IsDBNull(reader.GetOrdinal("additional_notes")) ? null : reader.GetString(reader.GetOrdinal("additional_notes")),
                    CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    LastDiscovered = reader.IsDBNull(reader.GetOrdinal("last_discovered")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("last_discovered")),
                    DiscoveryMethod = reader.IsDBNull(reader.GetOrdinal("discovery_method")) ? string.Empty : reader.GetString(reader.GetOrdinal("discovery_method")),
                    DeviceType = reader.IsDBNull(reader.GetOrdinal("device_type")) ? string.Empty : reader.GetString(reader.GetOrdinal("device_type")),
                    WebInterfaceUrl = reader.IsDBNull(reader.GetOrdinal("web_interface_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("web_interface_url")),
                    EquipmentGroup = reader.IsDBNull(reader.GetOrdinal("equipment_group")) ? string.Empty : reader.GetString(reader.GetOrdinal("equipment_group"))
                };
                devices.Add(device);
            }

            return devices;
        }
    }
}