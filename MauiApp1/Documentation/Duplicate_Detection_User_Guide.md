# Device Duplicate Detection and Merging System

## Overview

The IT Unified App now includes a comprehensive duplicate detection and device merging system that prevents duplicate device entries and provides intelligent data merging capabilities. This system is particularly useful for:

- **Asset Refresh Scenarios**: When physical hardware is replaced but maintains the same hostname/location
- **Network Changes**: When devices move between networks or get new IP addresses
- **Discovery Conflicts**: When automated scans detect devices that already exist in the database
- **Manual Entry Validation**: Preventing accidental duplicate entries through the admin interface

## Key Features

### 1. Multi-Level Duplicate Detection

The system checks for duplicates using multiple criteria with different confidence levels:

- **High Confidence Matches**:
  - Exact hostname match
  - Serial number match
  - Asset tag match

- **Medium Confidence Matches**:
  - MAC address match (any NIC)

- **Low Confidence Matches**:
  - IP address match (any NIC)

### 2. Resolution Actions

When duplicates are detected, users can choose from several resolution options:

- **Cancel**: Abort the operation
- **Create New**: Add as a new device despite duplicates
- **Update Existing**: Replace existing device data completely
- **Merge Data**: Selectively merge specific fields from new data into existing device

### 3. Field-Level Merging

The merge functionality supports granular field selection:

- **Individual Fields**: Update specific properties like hostname, manufacturer, model, etc.
- **Category Groups**: 
  - `all_hardware`: All hardware-related fields (CPU, RAM, storage, etc.)
  - `all_network`: All network-related fields (IP, MAC, DNS, etc.)
- **Preserve Existing**: Option to only update empty fields, preserving manually entered data

### 4. Comprehensive Audit Trail

All duplicate resolution actions are logged with:
- Windows User ID who performed the action
- Timestamp of the operation
- Reason for the resolution choice
- Detailed field-by-field change tracking
- Audit retention for 1 year

## Database Schema Enhancements

### New Methods in IDatabaseService

```csharp
// Duplicate detection methods
Task<List<Device>> GetDevicesByIpAddressAsync(string ipAddress);
Task<List<Device>> GetDevicesByMacAddressAsync(string macAddress);
Task<Device?> GetDeviceBySerialNumberAsync(string serialNumber);
Task<Device?> GetDeviceByAssetTagAsync(string assetTag);
Task<DuplicateDetectionResult> CheckForDuplicateDevicesAsync(Device device);
Task<bool> MergeDeviceDataAsync(Device existingDevice, Device newDevice, DuplicateResolutionOptions resolutionOptions);
```

### Enhanced AddDeviceService

```csharp
// New method with duplicate checking
Task<DeviceAddResult> AddDeviceWithDuplicateCheckAsync(
    Device device, 
    string deviceType = "Other", 
    bool checkDuplicates = true, 
    DuplicateResolutionOptions? resolutionOptions = null
);
```

## Usage Examples

### Basic Usage in Database Admin

```csharp
var result = await _addDeviceService.AddDeviceWithDuplicateCheckAsync(
    newDevice, 
    "Computer", 
    checkDuplicates: true, 
    resolutionOptions: null // Let user decide
);

if (result.DuplicatesFound)
{
    // Show user duplicate resolution dialog
    var userChoice = await ShowDuplicateResolutionDialog(result.DuplicateDetectionResult);
    
    // Re-attempt with user's choice
    var finalResult = await _addDeviceService.AddDeviceWithDuplicateCheckAsync(
        newDevice, "Computer", checkDuplicates: true, resolutionOptions: userChoice);
}
```

### Automated Hardware Refresh

```csharp
var resolutionOptions = new DuplicateResolutionOptions 
{ 
    Action = DuplicateResolutionAction.MergeData,
    FieldsToMerge = new List<string> { "all_hardware" },
    ResolutionReason = "Hardware refresh - device physically replaced",
    PreserveExistingData = true
};

var result = await _addDeviceService.AddDeviceWithDuplicateCheckAsync(
    newDeviceData, "Computer", checkDuplicates: true, resolutionOptions: resolutionOptions);
```

### Computer Scan Integration

```csharp
// For automated scans, merge hardware info while preserving manual asset data
var resolutionOptions = new DuplicateResolutionOptions 
{ 
    Action = DuplicateResolutionAction.MergeData,
    FieldsToMerge = new List<string> 
    { 
        "manufacturer", "model", "cpuinfo", "totalramgb", "ramtype", 
        "osname", "osversion", "primaryip", "primarymac"
    },
    ResolutionReason = "Automated computer scan update",
    PreserveExistingData = true
};
```

## Configuration Options

### Duplicate Detection Sensitivity

The system provides different match criteria:

1. **Strict Mode**: Only hostname and serial number matches
2. **Standard Mode** (default): Hostname, serial number, asset tag, and MAC address matches
3. **Comprehensive Mode**: All criteria including IP address matches

### Merge Policies

Pre-configured merge policies for common scenarios:

- **Asset Refresh**: Merge hardware specs, preserve asset management data
- **Network Update**: Merge network configuration, preserve everything else
- **Full Sync**: Merge all technical data, preserve manual annotations
- **Discovery Update**: Merge discovered data, preserve all manual entries

## Error Handling

The system includes comprehensive error handling:

- **Validation Errors**: Missing required fields, invalid data formats
- **Database Errors**: Connection issues, transaction failures
- **Merge Conflicts**: Irreconcilable data differences
- **Permission Errors**: Insufficient rights for operations

All errors are logged with full context for troubleshooting.

## Performance Considerations

### Indexing Strategy

The database includes optimized indexes for duplicate detection:

```sql
-- Hostname lookup (primary key for duplicate detection)
CREATE INDEX IX_devices_hostname ON devices(hostname);

-- Network interface searches
CREATE INDEX IX_devices_primary_ip ON devices(primary_ip);
CREATE INDEX IX_devices_primary_mac ON devices(primary_mac);
CREATE INDEX IX_devices_nic2_ip ON devices(nic2_ip);
CREATE INDEX IX_devices_nic2_mac ON devices(nic2_mac);
-- ... additional NIC indexes

-- Asset identification
CREATE INDEX IX_devices_serial_number ON devices(serial_number);
CREATE INDEX IX_devices_asset_tag ON devices(asset_tag);
```

### Batch Processing

For bulk imports or large scans:

```csharp
// Process devices in batches to avoid overwhelming the duplicate detection system
var batchSize = 50;
var devices = GetDevicesFromScan();

for (int i = 0; i < devices.Count; i += batchSize)
{
    var batch = devices.Skip(i).Take(batchSize);
    await ProcessDeviceBatch(batch);
}
```

## Security Features

### Audit Trail Integrity

All duplicate resolution actions are recorded with:
- Windows Authentication (SUSER_SNAME())
- Tamper-resistant timestamps
- Immutable audit log entries
- 1-year retention policy

### Access Control

The system respects existing role-based access controls:
- Read access required for duplicate detection
- Write access required for merge operations
- Admin access required for policy configuration

## Monitoring and Reporting

### Duplicate Detection Statistics

```sql
-- Query for duplicate detection activity
SELECT 
    action_type,
    COUNT(*) as resolution_count,
    COUNT(DISTINCT device_id) as devices_affected
FROM device_audit_log 
WHERE action_type IN ('MERGE', 'UPDATE', 'CREATE')
    AND performed_at >= DATEADD(DAY, -30, GETDATE())
GROUP BY action_type;
```

### Merge Quality Metrics

Track the effectiveness of merge operations:
- Success rate of automated merges
- Frequency of manual intervention required
- Most commonly merged fields
- User adoption of different resolution strategies

## Troubleshooting

### Common Issues

1. **False Positives**: Legitimate devices flagged as duplicates
   - Solution: Adjust detection sensitivity, refine matching criteria

2. **Merge Conflicts**: Data inconsistencies preventing clean merges
   - Solution: Manual review required, update merge policies

3. **Performance Impact**: Slow duplicate detection on large datasets
   - Solution: Optimize indexes, implement caching, batch processing

### Diagnostic Queries

```sql
-- Find devices with multiple potential duplicates
SELECT hostname, COUNT(*) as potential_duplicates
FROM (
    SELECT DISTINCT d1.hostname, d2.device_id
    FROM devices d1, devices d2
    WHERE d1.device_id != d2.device_id
    AND (d1.hostname = d2.hostname 
         OR d1.primary_ip = d2.primary_ip 
         OR d1.primary_mac = d2.primary_mac)
) duplicates
GROUP BY hostname
HAVING COUNT(*) > 1;
```

## Future Enhancements

### Planned Features

1. **Machine Learning Integration**: AI-powered duplicate detection using similarity algorithms
2. **Conflict Resolution Workflows**: Automated escalation for complex merge scenarios
3. **Integration APIs**: RESTful endpoints for external system integration
4. **Advanced Reporting**: Business intelligence dashboards for duplicate trends
5. **Policy Templates**: Pre-configured resolution strategies for different device types

### Configuration UI

Future versions will include:
- Visual duplicate detection configuration
- Merge policy designer
- Real-time duplicate detection dashboard
- Automated rule builder

## Conclusion

The duplicate detection and merging system provides a robust solution for maintaining data integrity in the IT device inventory. By combining intelligent detection algorithms with flexible resolution options, it ensures that the database remains accurate while minimizing administrative overhead.

For implementation questions or feature requests, refer to the example code in `Documentation/Duplicate_Detection_Implementation.cs` or contact the development team.
