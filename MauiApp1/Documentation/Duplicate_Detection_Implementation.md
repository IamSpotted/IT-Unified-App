# Duplicate Detection and Device Merging Functionality

This document demonstrates how to use the new duplicate detection and device merging functionality when adding devices to the IT device inventory system.

## Overview

The duplicate detection system provides intelligent conflict resolution when adding devices that may already exist in the database. It identifies potential duplicates based on multiple criteria and offers flexible resolution strategies.

## Key Components

### 1. IDuplicateResolutionDialogService

The main service for handling duplicate detection workflows:

```csharp
public interface IDuplicateResolutionDialogService
{
    Task<DuplicateResolutionResult> ShowDuplicateResolutionDialogAsync(
        Device newDevice, 
        Device existingDevice, 
        DuplicateResolutionOptions? options = null);
        
    Task ShowDuplicateResolutionResultAsync(
        DuplicateResolutionResult result, 
        string title = "Duplicate Resolution Complete");
}
```

### 2. DuplicateResolutionResult

Contains the user's choice and any merged device data:

```csharp
public class DuplicateResolutionResult
{
    public DuplicateResolutionAction Action { get; set; }
    public Device? MergedDevice { get; set; }
    public string? UserFeedback { get; set; }
    public bool Success { get; set; }
}
```

### 3. Resolution Actions

Available resolution strategies:

- **Cancel**: Abort the operation
- **CreateNew**: Add as a new device (ignore duplicate)
- **UpdateExisting**: Replace the existing device completely
- **MergeData**: Intelligently merge new and existing data

## Usage Examples

### Example 1: Basic Duplicate Checking

```csharp
public async Task<bool> AddDeviceWithBasicDuplicateCheckAsync(Device newDevice, string deviceType = "Other")
{
    try
    {
        // Attempt to add device with duplicate checking enabled
        var result = await _addDeviceService.AddDeviceWithDuplicateCheckAsync(
            newDevice, 
            deviceType, 
            checkDuplicates: true, 
            resolutionOptions: null // Ask user for resolution
        );

        if (result.Success)
        {
            await _dialogService.ShowAlertAsync("Success", $"Device '{newDevice.Hostname}' added successfully!");
            return true;
        }
        else
        {
            await _dialogService.ShowAlertAsync("Failed", $"Failed to add device: {result.Message}");
            return false;
        }
    }
    catch (Exception ex)
    {
        await _dialogService.ShowAlertAsync("Error", $"Error adding device: {ex.Message}");
        return false;
    }
}
```

### Example 2: Advanced Duplicate Detection with Custom Options

```csharp
public async Task<bool> AddDeviceWithAdvancedDuplicateDetectionAsync(Device newDevice)
{
    try
    {
        // Create custom resolution options
        var resolutionOptions = new DuplicateResolutionOptions
        {
            PreferManualData = true,  // Prefer manually entered data over scanned data
            AutoUpdateTimestamps = true,  // Update last_discovered when merging
            MergeNetworkInfo = true,  // Include all network interfaces in merge
            MergeStorageInfo = true,  // Include all storage drives in merge
            PreserveOriginalDates = false  // Update purchase/service dates if newer
        };

        var result = await _addDeviceService.AddDeviceWithDuplicateCheckAsync(
            newDevice, 
            "Computer", 
            checkDuplicates: true, 
            resolutionOptions: resolutionOptions
        );

        if (result.Success)
        {
            if (result.WasDuplicate && result.DuplicateResolution != null)
            {
                // Handle duplicate resolution result
                var resolutionResult = result.DuplicateResolution;
                string message = resolutionResult.Action switch
                {
                    DuplicateResolutionAction.UpdateExisting => "Existing device was updated with new information.",
                    DuplicateResolutionAction.MergeData => "Device data was intelligently merged with existing record.",
                    DuplicateResolutionAction.CreateNew => "Device was added as a new record (ignoring duplicate).",
                    _ => "Device was processed successfully."
                };
                
                await _dialogService.ShowAlertAsync("Duplicate Resolved", message);
            }
            else
            {
                await _dialogService.ShowAlertAsync("Success", "Device added successfully!");
            }
            return true;
        }
        else
        {
            await _dialogService.ShowAlertAsync("Failed", $"Failed to add device: {result.Message}");
            return false;
        }
    }
    catch (Exception ex)
    {
        await _dialogService.ShowAlertAsync("Error", $"Error in duplicate detection: {ex.Message}");
        return false;
    }
}
```

### Example 3: Computer Scanning with Duplicate Detection

```csharp
public async Task<bool> ScanAndAddComputerWithDuplicateDetectionAsync(string targetHostname)
{
    try
    {
        // Scan the computer first
        var scanResult = await ScanComputerAsync(targetHostname);
        if (!scanResult.Success || scanResult.ComputerInfo == null)
        {
            await _dialogService.ShowAlertAsync("Scan Failed", "Could not scan the target computer.");
            return false;
        }

        // Convert scan result to Device model
        var scannedDevice = ConvertScanToDeviceModel(scanResult.ComputerInfo);
        
        // Add with duplicate detection
        var addResult = await _addDeviceService.AddDeviceWithDuplicateCheckAsync(
            scannedDevice, 
            "Computer", 
            checkDuplicates: true,
            resolutionOptions: new DuplicateResolutionOptions
            {
                PreferManualData = false,  // Prefer fresh scan data
                AutoUpdateTimestamps = true,
                MergeNetworkInfo = true,
                MergeStorageInfo = true
            }
        );

        if (addResult.Success)
        {
            await _dialogService.ShowAlertAsync("Scan Complete", 
                $"Computer '{targetHostname}' has been scanned and added to the database.");
            return true;
        }
        else
        {
            await _dialogService.ShowAlertAsync("Failed", 
                $"Failed to add scanned computer: {addResult.Message}");
            return false;
        }
    }
    catch (Exception ex)
    {
        await _dialogService.ShowAlertAsync("Error", $"Error during computer scan: {ex.Message}");
        return false;
    }
}
```

## Integration with ViewModels

### DatabaseAdminViewModel Integration

The `DatabaseAdminViewModel` now includes duplicate detection in both manual device entry and scanning operations:

```csharp
[RelayCommand]
private async Task AddDevice()
{
    try
    {
        IsBusy = true;
        
        // Create device from form fields
        var device = new Models.Device
        {
            Hostname = Hostname,
            SerialNumber = Serial_number,
            // ... other properties
        };

        // Use duplicate detection service
        var result = await _addDeviceService.AddDeviceWithDuplicateCheckAsync(
            device, 
            device_type, 
            checkDuplicates: true
        );

        if (result.Success)
        {
            await _dialogService.ShowAlertAsync("Success", "Device added successfully!");
            ClearForm();
        }
    }
    finally
    {
        IsBusy = false;
    }
}
```

### ComputerInfoViewModel Integration

The `ComputerInfoViewModel` includes an `AddToDatabaseCommand` that uses duplicate detection:

```csharp
[RelayCommand]
private async Task AddComputerToDatabase()
{
    try
    {
        if (ComputerInfo == null) return;
        
        // Convert scan data to Device model
        var device = ConvertToDeviceModel(ComputerInfo);
        
        // Add with duplicate detection
        var result = await _addDeviceService.AddDeviceWithDuplicateCheckAsync(
            device, 
            "Computer", 
            checkDuplicates: true
        );

        if (result.Success)
        {
            await _dialogService.ShowAlertAsync("Success", 
                "Computer information has been added to the database!");
        }
    }
    catch (Exception ex)
    {
        await _dialogService.ShowAlertAsync("Error", $"Error adding to database: {ex.Message}");
    }
}
```

## Duplicate Detection Criteria

The system identifies potential duplicates based on:

1. **Hostname** (exact match, case-insensitive)
2. **Serial Number** (exact match)
3. **Asset Tag** (exact match)
4. **MAC Address** (any network interface)
5. **IP Address** (any network interface)

## Merge Strategy

When merging data, the system follows these rules:

1. **Preserve Manual Data**: Manually entered information takes precedence over scanned data
2. **Update Discovery Information**: Always update `last_discovered` and `discovery_method`
3. **Merge Network Interfaces**: Combine all unique network interfaces
4. **Merge Storage Drives**: Combine all unique storage drives
5. **Preserve Asset Management**: Keep purchase/service/warranty dates unless newer data is available

## Error Handling

The duplicate detection system includes comprehensive error handling:

- **Network Failures**: Graceful handling of database connectivity issues
- **Invalid Data**: Validation of device data before processing
- **User Cancellation**: Proper cleanup when user cancels operations
- **Concurrent Access**: Protection against simultaneous duplicate resolution

## Best Practices

1. **Always Enable Duplicate Checking**: Unless specifically adding test data
2. **Use Appropriate Resolution Options**: Configure based on data source (manual vs scanned)
3. **Handle Results Properly**: Check success status and provide user feedback
4. **Log Important Operations**: Use audit trail for duplicate resolutions
5. **Test Edge Cases**: Verify behavior with partial matches and missing data

## Future Enhancements

Potential improvements to the duplicate detection system:

1. **Fuzzy Matching**: Detect similar hostnames or serial numbers
2. **Confidence Scoring**: Assign confidence levels to duplicate matches
3. **Batch Processing**: Handle multiple devices with duplicate detection
4. **Custom Rules**: Allow administrators to define custom duplicate criteria
5. **Machine Learning**: Learn from user resolution choices to improve suggestions
