-- Enhanced IT Device Inventory Database
-- Updated to include all fields from GetComputerInfo script
-- Created: August 8, 2025
-- SQL Server Version for SSMS

-- Create the database (run this first, then connect to the new database)
CREATE DATABASE it_device_inventory;
GO

USE it_device_inventory;
GO

-- Main devices table with enhanced network configuration support
CREATE TABLE devices (
    device_id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Device Identity
    hostname NVARCHAR(255) UNIQUE NOT NULL,
    serial_number NVARCHAR(100),
    asset_tag NVARCHAR(50),
    device_type NVARCHAR(30),
    equipment_group NVARCHAR(50),

    -- Domain/Workgroup Information (NEW - from GetComputerInfo)
    domain_name NVARCHAR(100),
    workgroup NVARCHAR(100),
    is_domain_joined BIT DEFAULT 0,
    
    -- Hardware specs (from GetComputerInfo)
    manufacturer NVARCHAR(100),
    model NVARCHAR(100),
    cpu_info NVARCHAR(255),
    total_ram_gb DECIMAL(8,2),
    ram_type NVARCHAR(100),
    storage_info NVARCHAR(MAX), -- JSON array of drive details
    bios_version NVARCHAR(100),
    
    -- Software environment (from GetComputerInfo)
    os_name NVARCHAR(100),
    os_version NVARCHAR(50),
    os_architecture NVARCHAR(50),
    
    -- Network info (from GetComputerInfo)
    primary_ip NVARCHAR(45),
    primary_mac NVARCHAR(17),
    secondary_ips NVARCHAR(MAX), -- JSON array for multiple IPs
    secondary_macs NVARCHAR(MAX), -- JSON array for multiple MACs
    web_interface_url NVARCHAR(50),
    
    -- Network configuration (NEW - from GetComputerInfo)
    dns_servers NVARCHAR(MAX), -- JSON array of DNS servers
    default_gateways NVARCHAR(MAX), -- JSON array of gateways
    subnet_masks NVARCHAR(MAX), -- JSON array of subnet masks
    
    -- Manual entry - Primary location hierarchy
    device_status NVARCHAR(20) DEFAULT 'Active' CHECK (device_status IN ('Active', 'Inactive', 'Retired', 'Missing', 'Maintenance')),
    area NVARCHAR(100),
    zone NVARCHAR(100),
    line NVARCHAR(100),
    pitch NVARCHAR(100),
    
    -- Manual entry - Physical location (separated as requested)
    floor NVARCHAR(50),
    pillar NVARCHAR(50),
    
    -- Additional manual info
    additional_notes NVARCHAR(MAX),
    
    -- System fields
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    last_discovered DATETIME2,
    discovery_method NVARCHAR(50) -- 'GetComputerInfo', 'Manual', etc.
);
GO

-- Create indexes for fast searches
CREATE INDEX IX_devices_hostname ON devices(hostname);
CREATE INDEX IX_devices_primary_ip ON devices(primary_ip);
CREATE INDEX IX_devices_domain_name ON devices(domain_name);
CREATE INDEX IX_devices_area_zone ON devices(area, zone);
CREATE INDEX IX_devices_last_discovered ON devices(last_discovered);
CREATE INDEX IX_devices_device_status ON devices(device_status);
CREATE INDEX IX_devices_manufacturer_model ON devices(manufacturer, model);
GO

-- Audit log table for 6-month tracking
CREATE TABLE device_audit_log (
    log_id INT IDENTITY(1,1) PRIMARY KEY,
    device_id INT,
    action_type NVARCHAR(20) NOT NULL CHECK (action_type IN ('CREATE', 'UPDATE', 'DELETE', 'DISCOVER')),
    field_name NVARCHAR(100),
    old_value NVARCHAR(MAX),
    new_value NVARCHAR(MAX),
    performed_at DATETIME2 DEFAULT GETDATE(),
    discovery_session_id UNIQUEIDENTIFIER,
    
    FOREIGN KEY (device_id) REFERENCES devices(device_id) ON DELETE CASCADE
);
GO

-- Create indexes for audit log
CREATE INDEX IX_audit_device_id ON device_audit_log(device_id);
CREATE INDEX IX_audit_performed_at ON device_audit_log(performed_at);
CREATE INDEX IX_audit_session_id ON device_audit_log(discovery_session_id);
GO

-- Discovery sessions tracking
CREATE TABLE discovery_sessions (
    session_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    hostname_target NVARCHAR(255),
    started_at DATETIME2 DEFAULT GETDATE(),
    completed_at DATETIME2,
    status NVARCHAR(20) DEFAULT 'Running' CHECK (status IN ('Running', 'Completed', 'Failed')),
    results_summary NVARCHAR(MAX) -- JSON summary of what was discovered/changed
);
GO

-- Create indexes for discovery sessions
CREATE INDEX IX_sessions_started_at ON discovery_sessions(started_at);
CREATE INDEX IX_sessions_hostname_target ON discovery_sessions(hostname_target);
GO

-- Create a stored procedure to auto-purge old audit logs (6 months)
CREATE PROCEDURE CleanupAuditLogs
AS
BEGIN
    DELETE FROM device_audit_log 
    WHERE performed_at < DATEADD(MONTH, -6, GETDATE());
END;
GO

-- Create a trigger to automatically update the updated_at field
CREATE TRIGGER tr_devices_update_timestamp
ON devices
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE devices 
    SET updated_at = GETDATE()
    WHERE device_id IN (SELECT DISTINCT device_id FROM inserted);
END;
GO

-- Insert sample device for testing
INSERT INTO devices (
    hostname, 
    domain_name, 
    is_domain_joined,
    manufacturer, 
    model,
    device_status, 
    area, 
    zone, 
    discovery_method,
    additional_notes
) 
VALUES (
    'SAMPLE-WORKSTATION', 
    'CHATTPROD1.NA.VWG',
    1,
    'Dell Inc.', 
    'OptiPlex 7090',
    'Active', 
    'Production', 
    'A1',
    'GetComputerInfo',
    'Sample device for testing enhanced database structure'
);
GO

-- Sample queries for testing the new structure

-- Find devices by network configuration
/*
SELECT hostname, domain_name, dns_servers, default_gateways 
FROM devices 
WHERE dns_servers LIKE '%8.8.8.8%';
*/

-- Domain vs Workgroup breakdown
/*
SELECT 
    CASE WHEN is_domain_joined = 1 THEN 'Domain' ELSE 'Workgroup' END as join_type,
    COUNT(*) as device_count
FROM devices 
GROUP BY is_domain_joined;
*/

-- Devices by location hierarchy
/*
SELECT area, zone, line, COUNT(*) as device_count
FROM devices 
WHERE area IS NOT NULL 
GROUP BY area, zone, line
ORDER BY area, zone, line;
*/

-- Recent discoveries
/*
SELECT hostname, manufacturer, model, domain_name, last_discovered
FROM devices 
WHERE last_discovered >= DATEADD(DAY, -7, GETDATE())
ORDER BY last_discovered DESC;
*/

-- Show the enhanced table structure
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'devices'
ORDER BY c.ORDINAL_POSITION;
