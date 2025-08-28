using System.Data.SqlClient;
using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.Services
{
    /// <summary>
    /// This contains all the SQL Queries used throughout the application. Being centralized allows for easier troubleshooting of the SQL queries being used for CRUD operations
    /// as well as for the various pages that call on the device database to populate their lists of devices.
    /// </summary>
    public static class SQLQueryService
    {
        // This query is used to add a new device to the database
        public static string AddDeviceQuery => @"
            INSERT INTO dbo.devices (
                hostname, serial_number, asset_tag, domain_name, workgroup, is_domain_joined, manufacturer, model, cpu_info, total_ram_gb, ram_type, storage_info, bios_version,
                os_name, os_version, os_architecture, primary_ip, primary_mac, secondary_ips, secondary_macs, 
                dns_servers, default_gateways, subnet_masks, device_status, area, zone, line, pitch, floor, pillar,
                additional_notes, created_at, updated_at, last_discovered, discovery_method, device_type, web_interface_url, equipment_group
            ) VALUES (
                @hostname, @serial_number, @asset_tag, @domain_name, @workgroup, @is_domain_joined, @manufacturer, @model, @cpu_info, @total_ram_gb, @ram_type, @storage_info, @bios_version,
                @os_name, @os_version, @os_architecture, @primary_ip, @primary_mac, @secondary_ips, @secondary_macs,
                @dns_servers, @default_gateways, @subnet_masks, @device_status, @area, @zone, @line, @pitch, @floor, @pillar,
                @additional_notes, @created_at, @updated_at, @last_discovered, @discovery_method, @device_type, @web_interface_url, @equipment_group
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

        public static string RemoveDeviceByHostnameQuery => @"
            DELETE FROM dbo.devices
            WHERE hostname = @hostname";

        public static string GetAllDevicesQuery => @"
            SELECT * FROM dbo.devices
            ORDER BY hostname";

        public static string GetAllCamerasQuery => @"
        SELECT * FROM dbo.devices
        WHERE device_type = 'Camera'
        ORDER BY device_id";

        public static string GetAllPrintersQuery => @"
        SELECT * FROM dbo.devices
        WHERE device_type = 'Printer'
        ORDER BY device_id";

        public static string GetAllNetopsQuery => @"
        SELECT * FROM dbo.devices
        WHERE device_type IN ('PC', 'Server')
        ORDER BY device_id";

        public static string GetAllNetworkDevicesQuery => @"
        SELECT * FROM dbo.devices
        WHERE device_type IN ('Router', 'Switch')
        ORDER BY device_id";

        public static string UpdateDeviceQuery => @"
            UPDATE dbo.devices SET
                hostname = @hostname,
                serial_number = @serial_number,
                asset_tag = @asset_tag,
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
                device_type = @device_type,
                web_interface_url = @web_interface_url,
                equipment_group = @equipment_group
            WHERE device_id = @device_id";
    }
}