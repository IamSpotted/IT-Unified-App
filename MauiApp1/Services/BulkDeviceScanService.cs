using System.Diagnostics;
using MauiApp1.Interfaces;
using MauiApp1.Models;
using MauiApp1.Scripts;

namespace MauiApp1.Services
{
    /// <summary>
    /// Service for bulk device scanning operations from text file input
    /// </summary>
    public class BulkDeviceScanService : IBulkDeviceScanService, ITransientService
    {
        private readonly ILogger<BulkDeviceScanService> _logger;
        private readonly IAddDeviceService _addDeviceService;
        private readonly IDatabaseService _databaseService;

        public BulkDeviceScanService(
            ILogger<BulkDeviceScanService> logger,
            IAddDeviceService addDeviceService,
            IDatabaseService databaseService)
        {
            _logger = logger;
            _addDeviceService = addDeviceService;
            _databaseService = databaseService;
        }

        /// <summary>
        /// Processes a text file containing hostnames/IP addresses and adds devices to database
        /// </summary>
        public async Task<BulkScanResult> ProcessBulkScanAsync(string filePath, string applicationUser, Guid discoverySessionId, string changeReason)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                // Read all lines from the file
                var lines = await File.ReadAllLinesAsync(filePath);
                var hostnames = lines
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim())
                    .Where(line => !line.StartsWith("#")) // Allow comments
                    .ToList();

                _logger.LogInformation("Starting bulk scan from file: {FilePath} with {Count} hostnames", filePath, hostnames.Count);

                return await ProcessBulkScanAsync(hostnames, applicationUser, discoverySessionId, changeReason, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk scan from file: {FilePath}", filePath);
                return new BulkScanResult
                {
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    TotalProcessed = 0,
                    Failed = 1,
                    DeviceResults = new List<BulkScanDeviceResult>
                    {
                        new BulkScanDeviceResult
                        {
                            HostnameOrIP = filePath,
                            Success = false,
                            StatusMessage = "Failed to read file",
                            ErrorMessage = ex.Message,
                            Action = BulkScanDeviceAction.Failed
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Processes a list of hostnames/IP addresses and adds devices to database
        /// </summary>
        public async Task<BulkScanResult> ProcessBulkScanAsync(IEnumerable<string> hostnames, string applicationUser, Guid discoverySessionId, string changeReason, string? originalFilePath = null)
        {
            var result = new BulkScanResult
            {
                StartTime = DateTime.Now
            };

            var hostnamesList = hostnames.ToList();
            result.TotalProcessed = hostnamesList.Count;

            _logger.LogInformation("Starting bulk device scan for {Count} hostnames. Session: {SessionId}, User: {User}", 
                hostnamesList.Count, discoverySessionId, applicationUser);

            foreach (var hostname in hostnamesList)
            {
                var deviceResult = await ProcessSingleDeviceAsync(hostname, applicationUser, discoverySessionId, changeReason);
                result.DeviceResults.Add(deviceResult);

                // Update counters
                switch (deviceResult.Action)
                {
                    case BulkScanDeviceAction.Added:
                        result.SuccessfullyAdded++;
                        break;
                    case BulkScanDeviceAction.Failed:
                        result.Failed++;
                        break;
                    case BulkScanDeviceAction.Skipped:
                    case BulkScanDeviceAction.AlreadyExists:
                        result.Skipped++;
                        break;
                }

                // Add a small delay to avoid overwhelming the network/target systems
                await Task.Delay(1000);
            }

            result.EndTime = DateTime.Now;

            // Generate failure report if there are failed devices
            if (result.Failed > 0)
            {
                result.FailureReportPath = await GenerateFailureReportAsync(originalFilePath, result);
            }

            _logger.LogInformation("Bulk device scan completed. {Summary}", result.Summary);

            return result;
        }

        /// <summary>
        /// Processes a single device: scans it and adds to database
        /// </summary>
        private async Task<BulkScanDeviceResult> ProcessSingleDeviceAsync(string hostnameOrIP, string applicationUser, Guid discoverySessionId, string changeReason)
        {
            var stopwatch = Stopwatch.StartNew();
            var deviceResult = new BulkScanDeviceResult
            {
                HostnameOrIP = hostnameOrIP
            };

            try
            {
                _logger.LogDebug("Processing device: {HostnameOrIP}", hostnameOrIP);

                // Check if device already exists in database
                var existingDevice = await _databaseService.GetDeviceByHostnameAsync(hostnameOrIP);
                if (existingDevice != null)
                {
                    deviceResult.Success = false;
                    deviceResult.StatusMessage = "Device already exists in database";
                    deviceResult.Action = BulkScanDeviceAction.AlreadyExists;
                    deviceResult.DeviceId = existingDevice.device_id;
                    deviceResult.ResolvedComputerName = existingDevice.Hostname;
                    return deviceResult;
                }

                // Attempt to scan the device using the same service as individual scans
                var enhancedInfo = await ComputerInfoCollectionService.GetComputerInfoAsync(hostnameOrIP, true);

                if (enhancedInfo == null || string.IsNullOrEmpty(enhancedInfo.ComputerName))
                {
                    deviceResult.Success = false;
                    deviceResult.StatusMessage = "Failed to retrieve device information";
                    deviceResult.ErrorMessage = "Device scan returned no data or device is unreachable";
                    deviceResult.Action = BulkScanDeviceAction.Failed;
                    return deviceResult;
                }

                // Convert enhanced info to Device model
                var device = ConvertToDeviceModel(enhancedInfo, hostnameOrIP);

                // Set default values for bulk scan
                device.device_type = "PC"; // As specified, automatically assign PC as device type
                device.DeviceStatus = "Active"; // As specified, status is Active
                device.DiscoveryMethod = "Bulk Scan";
                device.LastDiscovered = DateTime.Now;
                device.CreatedAt = DateTime.Now;
                device.UpdatedAt = DateTime.Now;

                // Add device to database with audit logging
                var addSuccess = await _addDeviceService.AddDeviceAsync(
                    device, 
                    "PC", 
                    applicationUser, 
                    discoverySessionId, 
                    changeReason
                );

                if (addSuccess)
                {
                    deviceResult.Success = true;
                    deviceResult.StatusMessage = $"Successfully added device: {enhancedInfo.ComputerName}";
                    deviceResult.Action = BulkScanDeviceAction.Added;
                    deviceResult.DeviceId = device.device_id;
                    deviceResult.ResolvedComputerName = enhancedInfo.ComputerName;

                    _logger.LogInformation("Successfully added device {ComputerName} (ID: {DeviceId}) from bulk scan", 
                        enhancedInfo.ComputerName, device.device_id);
                }
                else
                {
                    deviceResult.Success = false;
                    deviceResult.StatusMessage = "Failed to add device to database";
                    deviceResult.ErrorMessage = "Database add operation failed";
                    deviceResult.Action = BulkScanDeviceAction.Failed;
                    deviceResult.ResolvedComputerName = enhancedInfo.ComputerName;
                }
            }
            catch (Exception ex)
            {
                deviceResult.Success = false;
                deviceResult.StatusMessage = "Error processing device";
                deviceResult.ErrorMessage = ex.Message;
                deviceResult.Action = BulkScanDeviceAction.Failed;

                _logger.LogError(ex, "Error processing device {HostnameOrIP} in bulk scan", hostnameOrIP);
            }
            finally
            {
                stopwatch.Stop();
                deviceResult.ProcessingTime = stopwatch.Elapsed;
            }

            return deviceResult;
        }

        /// <summary>
        /// Converts EnhancedComputerInfo to Device model for database insertion
        /// </summary>
        private Models.Device ConvertToDeviceModel(EnhancedComputerInfo enhancedInfo, string originalInput)
        {
            var device = new Models.Device
            {
                // Use the resolved computer name if available, otherwise use original input
                Hostname = !string.IsNullOrEmpty(enhancedInfo.ComputerName) ? enhancedInfo.ComputerName : originalInput,
                
                // Basic system information
                Manufacturer = enhancedInfo.Manufacturer,
                Model = enhancedInfo.Model,
                SerialNumber = enhancedInfo.SerialNumber,
                AssetTag = enhancedInfo.AssetTag,
                
                // Operating system information
                OsName = enhancedInfo.OperatingSystem,
                OSVersion = enhancedInfo.OSVersion,
                OsArchitecture = enhancedInfo.OSArchitecture,
                // Parse OS install date string to DateTime if possible
                OsInstallDate = DateTime.TryParse(enhancedInfo.OSInstallDate, out var installDate) ? installDate : null,
                
                // BIOS information
                BiosVersion = enhancedInfo.BIOSVersion,
                
                // Hardware information
                CpuInfo = enhancedInfo.ProcessorName,
                // Calculate total RAM from memory modules
                TotalRamGb = enhancedInfo.PhysicalMemory?.Any() == true ? 
                    (int)enhancedInfo.PhysicalMemory
                        .Where(m => !string.IsNullOrEmpty(m.Capacity) && m.Capacity.EndsWith("GB"))
                        .Sum(m => decimal.TryParse(m.Capacity.Replace("GB", ""), out var cap) ? cap : 0) : 0,
                RamType = enhancedInfo.RAMType,
                RamSpeed = enhancedInfo.PhysicalMemory?.FirstOrDefault()?.Speed,
                RamManufacturer = enhancedInfo.PhysicalMemory?.FirstOrDefault()?.Manufacturer,
                
                // Storage information from first physical disk
                StorageInfo = enhancedInfo.PhysicalDisks?.FirstOrDefault()?.DiskCapacity,
                StorageType = enhancedInfo.PhysicalDisks?.FirstOrDefault()?.DiskType,
                StorageModel = enhancedInfo.PhysicalDisks?.FirstOrDefault()?.DiskModel,
                
                // Domain information
                DomainName = enhancedInfo.ActiveDirectoryInfo?.DNSHostName,
                IsDomainJoined = enhancedInfo.ActiveDirectoryInfo != null,
                Workgroup = enhancedInfo.ActiveDirectoryInfo?.Name // Use AD name as workgroup fallback
            };

            // Network information - find primary adapter
            var connectedAdapters = enhancedInfo.NetworkAdapters?
                .Where(a => !string.IsNullOrEmpty(a.IPAddress) && 
                           a.IPAddress != "0.0.0.0" && 
                           !a.IPAddress.StartsWith("169.254") &&
                           (a.NetConnectionStatus == "Connected" || a.NetConnectionStatus == "2"))
                .ToList() ?? new List<EnhancedNetworkAdapter>();

            var primaryAdapter = DeterminePrimaryAdapter(connectedAdapters, originalInput);

            if (primaryAdapter != null)
            {
                device.PrimaryIp = primaryAdapter.IPAddress;
                device.PrimaryMac = primaryAdapter.FormattedMACAddress;
                device.PrimarySubnet = primaryAdapter.IPSubnet;
                
                // Parse DNS servers
                if (!string.IsNullOrEmpty(primaryAdapter.DNSServers))
                {
                    var dnsServers = primaryAdapter.DNSServers.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (dnsServers.Length > 0) device.PrimaryDns = dnsServers[0].Trim();
                    if (dnsServers.Length > 1) device.SecondaryDns = dnsServers[1].Trim();
                }
            }

            // Additional network adapters (up to 4 total)
            var otherAdapters = connectedAdapters.Where(a => a != primaryAdapter).Take(3).ToList();
            for (int i = 0; i < otherAdapters.Count; i++)
            {
                var adapter = otherAdapters[i];
                switch (i)
                {
                    case 0: // NIC2
                        device.Nic2Name = adapter.AdapterName;
                        device.Nic2Ip = adapter.IPAddress;
                        device.Nic2Mac = adapter.FormattedMACAddress;
                        device.Nic2Subnet = adapter.IPSubnet;
                        break;
                    case 1: // NIC3
                        device.Nic3Name = adapter.AdapterName;
                        device.Nic3Ip = adapter.IPAddress;
                        device.Nic3Mac = adapter.FormattedMACAddress;
                        device.Nic3Subnet = adapter.IPSubnet;
                        break;
                    case 2: // NIC4
                        device.Nic4Name = adapter.AdapterName;
                        device.Nic4Ip = adapter.IPAddress;
                        device.Nic4Mac = adapter.FormattedMACAddress;
                        device.Nic4Subnet = adapter.IPSubnet;
                        break;
                }
            }

            // Drive information (up to 4 drives)
            if (enhancedInfo.PhysicalDisks != null)
            {
                for (int i = 0; i < Math.Min(enhancedInfo.PhysicalDisks.Count, 4); i++)
                {
                    var disk = enhancedInfo.PhysicalDisks[i];
                    switch (i)
                    {
                        case 0: // Primary drive (already set above in StorageInfo)
                            // Primary drive information is already set in StorageInfo, StorageType, StorageModel
                            break;
                        case 1: // Drive 2
                            device.Drive2Name = disk.DiskName;
                            device.Drive2Capacity = disk.DiskCapacity;
                            device.Drive2Type = disk.DiskType;
                            device.Drive2Model = disk.DiskModel;
                            break;
                        case 2: // Drive 3
                            device.Drive3Name = disk.DiskName;
                            device.Drive3Capacity = disk.DiskCapacity;
                            device.Drive3Type = disk.DiskType;
                            device.Drive3Model = disk.DiskModel;
                            break;
                        case 3: // Drive 4
                            device.Drive4Name = disk.DiskName;
                            device.Drive4Capacity = disk.DiskCapacity;
                            device.Drive4Type = disk.DiskType;
                            device.Drive4Model = disk.DiskModel;
                            break;
                    }
                }
            }

            return device;
        }

        /// <summary>
        /// Determines the primary network adapter based on scan target
        /// </summary>
        private EnhancedNetworkAdapter? DeterminePrimaryAdapter(List<EnhancedNetworkAdapter> connectedAdapters, string scanTarget)
        {
            if (!connectedAdapters.Any()) return null;

            // If scan target is an IP address, try to find adapter with matching IP
            if (System.Net.IPAddress.TryParse(scanTarget, out var targetIP))
            {
                var matchingAdapter = connectedAdapters.FirstOrDefault(a => a.IPAddress == scanTarget);
                if (matchingAdapter != null) return matchingAdapter;
            }

            // Prefer Ethernet adapters over wireless
            var ethernetAdapter = connectedAdapters.FirstOrDefault(a => 
                a.AdapterName?.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) == true ||
                a.AdapterType?.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) == true);

            if (ethernetAdapter != null) return ethernetAdapter;

            // Return first connected adapter
            return connectedAdapters.FirstOrDefault();
        }

        /// <summary>
        /// Generates a text file report of devices that failed to scan
        /// </summary>
        private async Task<string?> GenerateFailureReportAsync(string? originalFilePath, BulkScanResult result)
        {
            try
            {
                if (string.IsNullOrEmpty(originalFilePath) || result.Failed == 0)
                {
                    return null;
                }

                // Create failure report file path
                var originalFileInfo = new FileInfo(originalFilePath);
                var failureReportPath = Path.Combine(
                    originalFileInfo.DirectoryName ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"{Path.GetFileNameWithoutExtension(originalFileInfo.Name)}_FAILED_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                );

                var failedDevices = result.DeviceResults
                    .Where(d => d.Action == BulkScanDeviceAction.Failed)
                    .ToList();

                if (failedDevices.Count == 0)
                {
                    return null;
                }

                // Create failure report content
                var reportLines = new List<string>
                {
                    $"# Bulk Device Scan Failure Report",
                    $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"# Original file: {originalFilePath}",
                    $"# Total devices processed: {result.TotalProcessed}",
                    $"# Failed devices: {result.Failed}",
                    $"# Successful devices: {result.SuccessfullyAdded}",
                    $"# Skipped devices: {result.Skipped}",
                    "",
                    "# Failed devices (you can copy these lines to retry):",
                    "# Format: hostname_or_ip    # Error: error_message",
                    ""
                };

                foreach (var failed in failedDevices)
                {
                    var errorMsg = !string.IsNullOrEmpty(failed.ErrorMessage) 
                        ? failed.ErrorMessage.Replace("\r", "").Replace("\n", " ")
                        : "Unknown error";
                    
                    reportLines.Add($"{failed.HostnameOrIP}    # Error: {errorMsg}");
                }

                // Add summary at the end
                reportLines.AddRange(new[]
                {
                    "",
                    "# Summary:",
                    $"# To retry failed devices, copy the device names above (without the '# Error:' comments)",
                    $"# and paste them into a new text file for bulk scanning.",
                    $"# Total failed devices in this report: {failedDevices.Count}"
                });

                await File.WriteAllLinesAsync(failureReportPath, reportLines);

                _logger.LogInformation("Generated failure report: {FailureReportPath} with {FailedCount} failed devices", 
                    failureReportPath, failedDevices.Count);

                return failureReportPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating failure report");
                return null;
            }
        }
    }
}