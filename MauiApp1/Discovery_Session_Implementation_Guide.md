# Application Session ID Implementation Guide

## Overview
The application now uses a **static session ID** that persists for the entire application lifecycle. This provides better tracking of user sessions and operations performed during a single "use" of the application.

## How Application Session ID Works

### **Static Session Generation**
```csharp
// Generated ONCE when the application starts
public static class ApplicationSession
{
    private static readonly Guid _sessionId = Guid.NewGuid();
    private static readonly DateTime _sessionStartTime = DateTime.Now;
    
    public static Guid SessionId => _sessionId;        // Same ID until app closes
    public static DateTime SessionStartTime => _sessionStartTime;
    public static string SessionInfo => $"Session {_sessionId} started at {_sessionStartTime:yyyy-MM-dd HH:mm:ss}";
    public static TimeSpan SessionDuration => DateTime.Now - _sessionStartTime;
}
```

### **Automatic Session Tracking**
1. **App Startup**: Unique session ID generated when app launches
2. **All Operations**: Use the same session ID automatically
3. **App Close**: Session ends, next launch gets new session ID

### **Session Lifecycle**
```
App Launch → ApplicationSession.SessionId = "12345678-1234-1234-1234-123456789ABC"
├── User adds device → Uses session ID "12345678-..."  
├── User updates device → Uses session ID "12345678-..."
├── User deletes device → Uses session ID "12345678-..."
└── App Close

App Launch → ApplicationSession.SessionId = "ABCDEFGH-5678-5678-5678-ABCDEFGHIJKL" (NEW)
├── User operations → Uses NEW session ID "ABCDEFGH-..."
```

## Updated Method Behavior

### **Add Device**
```csharp
// Method signature (same as before)
public async Task<bool> AddDeviceAsync(
    Device device, 
    string deviceType, 
    string applicationUser, 
    Guid? discoverySessionId,    // Optional override
    string changeReason
)

// Behavior:
// - If discoverySessionId is null → Uses ApplicationSession.SessionId
// - If discoverySessionId is provided → Uses the provided ID (for special batch operations)
```

### **Delete Device**
```csharp
// Method signature (same as before)
public async Task<bool> DeleteDeviceAsync(
    int deviceId, 
    string? deletionReason = null, 
    Guid? discoverySessionId = null,    // Optional override
    string? applicationUser = null
)

// Behavior:
// - If discoverySessionId is null → Uses ApplicationSession.SessionId
// - If discoverySessionId is provided → Uses the provided ID
```

## Usage Examples

### **Normal Operations (Uses Static Session)**
```csharp
// All these operations will use the same ApplicationSession.SessionId
await databaseService.AddDeviceAsync(device1, "Computer", "John Doe", null, "Manual entry");
await databaseService.AddDeviceAsync(device2, "Server", "John Doe", null, "Manual entry");
await databaseService.DeleteDeviceAsync(deviceId, "Decommissioned", null, "John Doe");
```

### **Special Batch Operations (Custom Session)**
```csharp
// Create a special session for a specific batch operation
Guid batchScanId = Guid.NewGuid();

foreach (var device in discoveredDevices)
{
    await databaseService.AddDeviceAsync(device, "Computer", "Discovery Service", batchScanId, "Network scan batch");
}
```

### **Mixed Usage**
```csharp
// Manual operations use application session
await databaseService.AddDeviceAsync(manualDevice, "Server", "IT Admin", null, "Manual server setup");

// Batch scan uses custom session  
Guid scanSession = Guid.NewGuid();
foreach (var device in scanResults)
{
    await databaseService.AddDeviceAsync(device, "Computer", "Auto Discovery", scanSession, "Automated scan");
}

// Back to manual operations (uses application session again)
await databaseService.DeleteDeviceAsync(oldDeviceId, "Replaced by new server", null, "IT Admin");
```

## Database Benefits

### **Session-Based Queries**
```sql
-- Find all operations from the current application session
SELECT * FROM device_audit_log 
WHERE discovery_session_id = @CurrentApplicationSessionId
ORDER BY performed_at;

-- Analyze user session activity
SELECT 
    COUNT(*) as total_operations,
    COUNT(CASE WHEN action_type = 'CREATE' THEN 1 END) as devices_added,
    COUNT(CASE WHEN action_type = 'UPDATE' THEN 1 END) as devices_updated,
    COUNT(CASE WHEN action_type = 'DELETE' THEN 1 END) as devices_deleted,
    MIN(performed_at) as session_start,
    MAX(performed_at) as session_end,
    performed_by as user_name
FROM device_audit_log 
WHERE discovery_session_id = @SessionId
GROUP BY performed_by;
```

### **Session Timeline Analysis**
```sql
-- View chronological session activity
SELECT 
    performed_at,
    action_type,
    field_name,
    CASE 
        WHEN action_type = 'CREATE' THEN 'Added: ' + COALESCE(new_value, 'Device')
        WHEN action_type = 'UPDATE' THEN 'Updated: ' + COALESCE(field_name, 'Field')
        WHEN action_type = 'DELETE' THEN 'Deleted: ' + COALESCE(old_value, 'Device')
    END as operation_summary,
    performed_by,
    change_reason
FROM device_audit_log 
WHERE discovery_session_id = @SessionId
ORDER BY performed_at;
```

### **Multi-Session Comparison**
```sql
-- Compare different application sessions
SELECT 
    discovery_session_id,
    COUNT(*) as operations,
    MIN(performed_at) as session_start,
    MAX(performed_at) as session_end,
    DATEDIFF(MINUTE, MIN(performed_at), MAX(performed_at)) as session_duration_minutes
FROM device_audit_log 
WHERE performed_at >= DATEADD(DAY, -7, GETDATE())
    AND discovery_session_id IS NOT NULL
GROUP BY discovery_session_id
ORDER BY session_start DESC;
```

## Key Advantages

### **1. User Session Tracking**
- See everything a user did in one app session
- Track session duration and activity patterns
- Identify productive vs. brief sessions

### **2. Simplified Usage**
- No need to manually generate session IDs for normal operations
- Automatic session tracking for 90% of use cases
- Option to override for special batch operations

### **3. Better Analytics**
- Group operations by user work sessions
- Analyze workflow patterns
- Track application usage statistics

### **4. Debugging Benefits**
- Trace all operations from a problematic session
- Correlate issues with specific application launches
- Better error investigation capabilities

## Implementation Notes

### **Session Initialization**
- Session ID generated in `App()` constructor
- Logged to Debug output and Console
- Available immediately when app starts

### **Backward Compatibility**
- All existing method calls continue to work
- `null` session ID automatically uses application session
- No breaking changes to existing code

### **Performance**
- Single static GUID - no generation overhead
- Minimal memory footprint
- No impact on database operations

### **Thread Safety**
- Static readonly fields are thread-safe
- No locking required
- Safe for concurrent operations

## Example Session Output

When you launch the app, you'll see:
```
IT Unified App started - Session 12345678-1234-1234-1234-123456789ABC started at 2025-09-02 14:30:15
```

All operations during this session will use ID `12345678-1234-1234-1234-123456789ABC` until you close and reopen the app.
