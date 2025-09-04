-- Update triggers for improved audit logging
-- Run this to fix the existing database triggers

USE device_inventory;
GO

-- Drop existing triggers to recreate them
IF OBJECT_ID('tr_devices_audit_insert', 'TR') IS NOT NULL
    DROP TRIGGER tr_devices_audit_insert;
GO

IF OBJECT_ID('tr_devices_audit_update', 'TR') IS NOT NULL
    DROP TRIGGER tr_devices_audit_update;
GO

IF OBJECT_ID('tr_devices_audit_delete', 'TR') IS NOT NULL
    DROP TRIGGER tr_devices_audit_delete;
GO

-- Recreate INSERT trigger (single comprehensive row)
CREATE TRIGGER tr_devices_audit_insert
ON devices
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO device_audit_log (
        device_id, 
        action_type, 
        field_name, 
        old_value, 
        new_value, 
        performed_at, 
        performed_by, 
        application_user, 
        change_reason
    )
    SELECT 
        i.device_id,
        'CREATE',
        'DEVICE_CREATED',
        NULL,
        'Device created: ' + i.hostname + ' (' + ISNULL(i.manufacturer, 'Unknown') + ' ' + ISNULL(i.model, 'Unknown') + ')',
        GETDATE(),
        SUSER_SNAME(),
        SUSER_SNAME(), -- Application user same as Windows user for device creation
        'New device added to inventory'
    FROM inserted i;
END;
GO

-- Recreate UPDATE trigger (with application_user)
CREATE TRIGGER tr_devices_audit_update
ON devices
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update the updated_at timestamp
    UPDATE devices 
    SET updated_at = GETDATE()
    WHERE device_id IN (SELECT DISTINCT device_id FROM inserted);
    
    -- Log detailed field changes
    INSERT INTO device_audit_log (device_id, action_type, field_name, old_value, new_value, performed_at, performed_by, application_user)
    SELECT 
        device_id,
        'UPDATE',
        field_name,
        old_value,
        new_value,
        GETDATE(),
        SUSER_SNAME(),
        SUSER_SNAME() -- Application user same as Windows user for updates
    FROM (
        -- Compare all relevant fields
        SELECT i.device_id, 'hostname' as field_name, d.hostname as old_value, i.hostname as new_value FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.hostname, '') != ISNULL(i.hostname, '')
        UNION ALL
        SELECT i.device_id, 'serial_number', d.serial_number, i.serial_number FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.serial_number, '') != ISNULL(i.serial_number, '')
        UNION ALL
        SELECT i.device_id, 'asset_tag', d.asset_tag, i.asset_tag FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.asset_tag, '') != ISNULL(i.asset_tag, '')
        UNION ALL
        SELECT i.device_id, 'device_type', d.device_type, i.device_type FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.device_type, '') != ISNULL(i.device_type, '')
        UNION ALL
        SELECT i.device_id, 'equipment_group', d.equipment_group, i.equipment_group FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.equipment_group, '') != ISNULL(i.equipment_group, '')
        UNION ALL
        SELECT i.device_id, 'domain_name', d.domain_name, i.domain_name FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.domain_name, '') != ISNULL(i.domain_name, '')
        UNION ALL
        SELECT i.device_id, 'is_domain_joined', CAST(d.is_domain_joined as NVARCHAR), CAST(i.is_domain_joined as NVARCHAR) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.is_domain_joined, 0) != ISNULL(i.is_domain_joined, 0)
        UNION ALL
        SELECT i.device_id, 'manufacturer', d.manufacturer, i.manufacturer FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.manufacturer, '') != ISNULL(i.manufacturer, '')
        UNION ALL
        SELECT i.device_id, 'model', d.model, i.model FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.model, '') != ISNULL(i.model, '')
        UNION ALL
        SELECT i.device_id, 'cpu_info', d.cpu_info, i.cpu_info FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.cpu_info, '') != ISNULL(i.cpu_info, '')
        UNION ALL
        SELECT i.device_id, 'total_ram_gb', CAST(d.total_ram_gb as NVARCHAR), CAST(i.total_ram_gb as NVARCHAR) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.total_ram_gb, 0) != ISNULL(i.total_ram_gb, 0)
        UNION ALL
        SELECT i.device_id, 'ram_type', d.ram_type, i.ram_type FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.ram_type, '') != ISNULL(i.ram_type, '')
        UNION ALL
        SELECT i.device_id, 'ram_speed', d.ram_speed, i.ram_speed FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.ram_speed, '') != ISNULL(i.ram_speed, '')
        UNION ALL
        SELECT i.device_id, 'ram_manufacturer', d.ram_manufacturer, i.ram_manufacturer FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.ram_manufacturer, '') != ISNULL(i.ram_manufacturer, '')
        UNION ALL
        SELECT i.device_id, 'os_name', d.os_name, i.os_name FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.os_name, '') != ISNULL(i.os_name, '')
        UNION ALL
        SELECT i.device_id, 'os_version', d.os_version, i.os_version FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.os_version, '') != ISNULL(i.os_version, '')
        UNION ALL
        SELECT i.device_id, 'os_architecture', d.os_architecture, i.os_architecture FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.os_architecture, '') != ISNULL(i.os_architecture, '')
        UNION ALL
        SELECT i.device_id, 'primary_ip', d.primary_ip, i.primary_ip FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.primary_ip, '') != ISNULL(i.primary_ip, '')
        UNION ALL
        SELECT i.device_id, 'primary_mac', d.primary_mac, i.primary_mac FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.primary_mac, '') != ISNULL(i.primary_mac, '')
        UNION ALL
        SELECT i.device_id, 'primary_subnet', d.primary_subnet, i.primary_subnet FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.primary_subnet, '') != ISNULL(i.primary_subnet, '')
        UNION ALL
        SELECT i.device_id, 'primary_dns', d.primary_dns, i.primary_dns FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.primary_dns, '') != ISNULL(i.primary_dns, '')
        UNION ALL
        SELECT i.device_id, 'secondary_dns', d.secondary_dns, i.secondary_dns FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.secondary_dns, '') != ISNULL(i.secondary_dns, '')
        UNION ALL
        SELECT i.device_id, 'device_status', d.device_status, i.device_status FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.device_status, '') != ISNULL(i.device_status, '')
        UNION ALL
        SELECT i.device_id, 'area', d.area, i.area FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.area, '') != ISNULL(i.area, '')
        UNION ALL
        SELECT i.device_id, 'zone', d.zone, i.zone FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.zone, '') != ISNULL(i.zone, '')
        UNION ALL
        SELECT i.device_id, 'line', d.line, i.line FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.line, '') != ISNULL(i.line, '')
        UNION ALL
        SELECT i.device_id, 'purchase_date', CONVERT(NVARCHAR, d.purchase_date, 120), CONVERT(NVARCHAR, i.purchase_date, 120) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE d.purchase_date != i.purchase_date OR (d.purchase_date IS NULL AND i.purchase_date IS NOT NULL) OR (d.purchase_date IS NOT NULL AND i.purchase_date IS NULL)
        UNION ALL
        SELECT i.device_id, 'service_date', CONVERT(NVARCHAR, d.service_date, 120), CONVERT(NVARCHAR, i.service_date, 120) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE d.service_date != i.service_date OR (d.service_date IS NULL AND i.service_date IS NOT NULL) OR (d.service_date IS NOT NULL AND i.service_date IS NULL)
        UNION ALL
        SELECT i.device_id, 'warranty_date', CONVERT(NVARCHAR, d.warranty_date, 120), CONVERT(NVARCHAR, i.warranty_date, 120) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE d.warranty_date != i.warranty_date OR (d.warranty_date IS NULL AND i.warranty_date IS NOT NULL) OR (d.warranty_date IS NOT NULL AND i.warranty_date IS NULL)
    ) changes;
END;
GO

-- Recreate DELETE trigger (single comprehensive row)
CREATE TRIGGER tr_devices_audit_delete
ON devices
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Archive each deleted device to deleted_devices table
    INSERT INTO deleted_devices (
        original_device_id, hostname, serial_number, asset_tag, device_type, equipment_group,
        domain_name, is_domain_joined, manufacturer, model, cpu_info, bios_version,
        total_ram_gb, ram_type, ram_speed, ram_manufacturer,
        os_name, os_version, os_architecture, os_install_date,
        storage_info, storage_type, storage_model,
        drive2_name, drive2_capacity, drive2_type, drive2_model,
        drive3_name, drive3_capacity, drive3_type, drive3_model,
        drive4_name, drive4_capacity, drive4_type, drive4_model,
        primary_ip, primary_mac, primary_subnet, primary_dns, secondary_dns,
        nic2_name, nic2_ip, nic2_mac, nic2_subnet,
        nic3_name, nic3_ip, nic3_mac, nic3_subnet,
        nic4_name, nic4_ip, nic4_mac, nic4_subnet,
        web_interface_url, device_status, area, zone, line, pitch, floor, pillar,
        additional_notes, purchase_date, service_date, warranty_date,
        original_created_at, original_updated_at, last_discovered, discovery_method,
        deleted_by, deletion_reason
    )
    SELECT 
        d.device_id, d.hostname, d.serial_number, d.asset_tag, d.device_type, d.equipment_group,
        d.domain_name, d.is_domain_joined, d.manufacturer, d.model, d.cpu_info, d.bios_version,
        d.total_ram_gb, d.ram_type, d.ram_speed, d.ram_manufacturer,
        d.os_name, d.os_version, d.os_architecture, d.os_install_date,
        d.storage_info, d.storage_type, d.storage_model,
        d.drive2_name, d.drive2_capacity, d.drive2_type, d.drive2_model,
        d.drive3_name, d.drive3_capacity, d.drive3_type, d.drive3_model,
        d.drive4_name, d.drive4_capacity, d.drive4_type, d.drive4_model,
        d.primary_ip, d.primary_mac, d.primary_subnet, d.primary_dns, d.secondary_dns,
        d.nic2_name, d.nic2_ip, d.nic2_mac, d.nic2_subnet,
        d.nic3_name, d.nic3_ip, d.nic3_mac, d.nic3_subnet,
        d.nic4_name, d.nic4_ip, d.nic4_mac, d.nic4_subnet,
        d.web_interface_url, d.device_status, d.area, d.zone, d.line, d.pitch, d.floor, d.pillar,
        d.additional_notes, d.purchase_date, d.service_date, d.warranty_date,
        d.created_at, d.updated_at, d.last_discovered, d.discovery_method,
        SUSER_SNAME(), 'Device deleted via application'
    FROM deleted d;
    
    -- Log the deletion in audit trail (single comprehensive entry)
    INSERT INTO device_audit_log (
        device_id, 
        action_type, 
        field_name, 
        old_value, 
        new_value, 
        performed_at, 
        performed_by, 
        application_user, 
        change_reason
    )
    SELECT 
        d.device_id,
        'DELETE',
        'DEVICE_DELETED',
        'Device removed: ' + d.hostname + ' (' + ISNULL(d.manufacturer, 'Unknown') + ' ' + ISNULL(d.model, 'Unknown') + ') - IP: ' + ISNULL(d.primary_ip, 'N/A'),
        'Device archived to deleted_devices table',
        GETDATE(),
        SUSER_SNAME(),
        SUSER_SNAME(), -- Application user same as Windows user for deletion
        'Device deleted via application' -- This will be updated by the application with user-provided reason
    FROM deleted d;
    
    -- Now actually delete from the main table
    DELETE FROM devices WHERE device_id IN (SELECT device_id FROM deleted);
END;
GO

PRINT 'All triggers updated successfully!';
PRINT 'Changes made:';
PRINT '1. INSERT trigger now creates single comprehensive audit entry with application_user';
PRINT '2. UPDATE trigger now includes application_user field';
PRINT '3. DELETE trigger creates single comprehensive audit entry';
PRINT '4. Application will update deletion audit entry with user-provided reason';
