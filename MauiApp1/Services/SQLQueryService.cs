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
                hostname, serial_number, asset_tag, domain_name, is_domain_joined, manufacturer, model, cpu_info, 
                bios_version, total_ram_gb, ram_type, ram_speed, ram_manufacturer, os_name, os_version, os_architecture, 
                os_install_date, storage_info, storage_type, storage_model, drive2_name, drive2_capacity, drive2_type, drive2_model,
                drive3_name, drive3_capacity, drive3_type, drive3_model, drive4_name, drive4_capacity, drive4_type, drive4_model,
                primary_ip, primary_mac, primary_subnet, primary_dns, secondary_dns, nic2_name, nic2_ip, nic2_mac, nic2_subnet,
                nic3_name, nic3_ip, nic3_mac, nic3_subnet, nic4_name, nic4_ip, nic4_mac, nic4_subnet, web_interface_url,
                device_status, area, zone, line, pitch, floor, pillar, additional_notes, created_at, updated_at, 
                last_discovered, discovery_method, device_type, equipment_group
            ) VALUES (
                @hostname, @serial_number, @asset_tag, @domain_name, @is_domain_joined, @manufacturer, @model, @cpu_info,
                @bios_version, @total_ram_gb, @ram_type, @ram_speed, @ram_manufacturer, @os_name, @os_version, @os_architecture,
                @os_install_date, @storage_info, @storage_type, @storage_model, @drive2_name, @drive2_capacity, @drive2_type, @drive2_model,
                @drive3_name, @drive3_capacity, @drive3_type, @drive3_model, @drive4_name, @drive4_capacity, @drive4_type, @drive4_model,
                @primary_ip, @primary_mac, @primary_subnet, @primary_dns, @secondary_dns, @nic2_name, @nic2_ip, @nic2_mac, @nic2_subnet,
                @nic3_name, @nic3_ip, @nic3_mac, @nic3_subnet, @nic4_name, @nic4_ip, @nic4_mac, @nic4_subnet, @web_interface_url,
                @device_status, @area, @zone, @line, @pitch, @floor, @pillar, @additional_notes, @created_at, @updated_at,
                @last_discovered, @discovery_method, @device_type, @equipment_group
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

        public static string SearchDevicesQuery => @"
            SELECT * FROM dbo.devices
            WHERE 
                hostname LIKE @searchTerm OR
                primary_ip LIKE @searchTerm OR
                primary_mac LIKE @searchTerm OR
                area LIKE @searchTerm OR
                zone LIKE @searchTerm OR
                line LIKE @searchTerm OR
                serial_number LIKE @searchTerm OR
                asset_tag LIKE @searchTerm OR
                device_type LIKE @searchTerm OR
                device_status LIKE @searchTerm OR
                manufacturer LIKE @searchTerm OR
                model LIKE @searchTerm OR
                additional_notes LIKE @searchTerm
            ORDER BY hostname";

        public static string GetDeviceByHostnameQuery => @"
            SELECT TOP 1 * FROM devices WHERE hostname = @hostname";

        public static string UpdateDeviceQuery => @"
            UPDATE dbo.devices SET
                hostname = @hostname,
                serial_number = @serial_number,
                asset_tag = @asset_tag,
                domain_name = @domain_name,
                is_domain_joined = @is_domain_joined,
                manufacturer = @manufacturer,
                model = @model,
                cpu_info = @cpu_info,
                bios_version = @bios_version,
                total_ram_gb = @total_ram_gb,
                ram_type = @ram_type,
                ram_speed = @ram_speed,
                ram_manufacturer = @ram_manufacturer,
                os_name = @os_name,
                os_version = @os_version,
                os_architecture = @os_architecture,
                os_install_date = @os_install_date,
                storage_info = @storage_info,
                storage_type = @storage_type,
                storage_model = @storage_model,
                drive2_name = @drive2_name,
                drive2_capacity = @drive2_capacity,
                drive2_type = @drive2_type,
                drive2_model = @drive2_model,
                drive3_name = @drive3_name,
                drive3_capacity = @drive3_capacity,
                drive3_type = @drive3_type,
                drive3_model = @drive3_model,
                drive4_name = @drive4_name,
                drive4_capacity = @drive4_capacity,
                drive4_type = @drive4_type,
                drive4_model = @drive4_model,
                primary_ip = @primary_ip,
                primary_mac = @primary_mac,
                primary_subnet = @primary_subnet,
                primary_dns = @primary_dns,
                secondary_dns = @secondary_dns,
                nic2_name = @nic2_name,
                nic2_ip = @nic2_ip,
                nic2_mac = @nic2_mac,
                nic2_subnet = @nic2_subnet,
                nic3_name = @nic3_name,
                nic3_ip = @nic3_ip,
                nic3_mac = @nic3_mac,
                nic3_subnet = @nic3_subnet,
                nic4_name = @nic4_name,
                nic4_ip = @nic4_ip,
                nic4_mac = @nic4_mac,
                nic4_subnet = @nic4_subnet,
                web_interface_url = @web_interface_url,
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
                equipment_group = @equipment_group
            WHERE device_id = @device_id";
    }
}