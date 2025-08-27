using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

using System.Windows.Input;
using Microsoft.Win32;
using SystemNet = System.Net;
using System.Linq;
using MauiApp1.Models;
using MauiApp1.Interfaces;
using DeviceModel = MauiApp1.Models.Device;
using System.Diagnostics;

namespace MauiApp1.ViewModels
{

        public class ComputerInfoViewModel : INotifyPropertyChanged
        {
            public ICommand UpdateSelectedCommand { get; }
            public ICommand UpdateAllCommand { get; }

            private string _targetInput = string.Empty;
            private string _results = string.Empty;
            private string _statusMessage = string.Empty;
            private bool _isScanning = false;
            private bool _hasResults = false;

            private readonly IDatabaseService _databaseService;
            private ObservableCollection<ComputerInfo> _computerResults = new();
            private ObservableCollection<ComputerInfoComparisonResult> _comparisonResults = new();


        public ComputerInfoViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            ScanCommand = new Command(async () => await ScanComputersAsync());
            ClearCommand = new Command(ClearResults);

            UpdateSelectedCommand = new Command<ComputerInfoComparisonResult>(async (comparison) => await UpdateSelectedFieldsAsync(comparison));
            UpdateAllCommand = new Command<ComputerInfoComparisonResult>(async (comparison) => await UpdateAllFieldsAsync(comparison));
        }
        // Update only checked fields
        private async Task UpdateSelectedFieldsAsync(ComputerInfoComparisonResult comparison)
        {
            if (comparison == null || comparison.DatabaseInfo == null) return;
            var dbDevice = comparison.DatabaseInfo;
            var script = comparison.ScriptInfo;

            // Only update fields where the corresponding boolean is true
            if (comparison.UpdateSerialNumber) dbDevice.SerialNumber = script.SerialNumber;
            if (comparison.UpdateAssetTag) dbDevice.AssetTag = script.AssetTag;
            if (comparison.UpdateDomainName) dbDevice.DomainName = script.DomainOrWorkgroupDisplay;
            if (comparison.UpdateWorkgroup) dbDevice.Workgroup = script.DomainOrWorkgroupDisplay;
            if (comparison.UpdateIsDomainJoined) dbDevice.IsDomainJoined = script.IsDomainJoined;
            if (comparison.UpdateManufacturer) dbDevice.Manufacturer = script.Manufacturer;
            if (comparison.UpdateModel) dbDevice.Model = script.Model;
            if (comparison.UpdateCpuInfo) dbDevice.CpuInfo = script.ProcessorName;
            if (comparison.UpdateTotalRamGb) dbDevice.TotalRamGb = (decimal)script.RAMInstalledGB;
            if (comparison.UpdateRamType) dbDevice.RamType = script.RAMType;
            if (comparison.UpdateStorageInfo) dbDevice.StorageInfo = script.DriveDetails != null ? string.Join("; ", script.DriveDetails.Select(d => d.SizeInfo)) : null;
            if (comparison.UpdateBiosVersion) dbDevice.BiosVersion = script.BIOSVersion;
            if (comparison.UpdateOsName) dbDevice.OsName = script.OperatingSystem;
            if (comparison.UpdateOsVersion) dbDevice.OSVersion = script.OSVersion;
            if (comparison.UpdateOsArchitecture) dbDevice.OsArchitecture = script.OSArchitecture;
            if (comparison.UpdatePrimaryIp) dbDevice.IpAddress = script.NetworkAdapters?.FirstOrDefault()?.IPAddress;
            if (comparison.UpdatePrimaryMac) dbDevice.MacAddress = script.NetworkAdapters?.FirstOrDefault()?.MACAddress;
            if (comparison.UpdateSecondaryIps) dbDevice.SecondaryIps = script.NetworkAdapters != null ? string.Join(",", script.NetworkAdapters.Skip(1).Select(a => a.IPAddress)) : null;
            if (comparison.UpdateSecondaryMacs) dbDevice.SecondaryMacs = script.NetworkAdapters != null ? string.Join(",", script.NetworkAdapters.Skip(1).Select(a => a.MACAddress)) : null;
            if (comparison.UpdateDnsServers) dbDevice.DnsServers = script.DNSServersDisplay;
            if (comparison.UpdateDefaultGateways) dbDevice.DefaultGateways = script.DefaultGatewaysDisplay;
            if (comparison.UpdateSubnetMasks) dbDevice.SubnetMasks = script.SubnetMasksDisplay;

            await _databaseService.UpdateDeviceAsync(dbDevice);
            StatusMessage = $"Updated selected fields for {dbDevice.Hostname}.";
        }

        // Update all fields (set all booleans to true, then update)
        private async Task UpdateAllFieldsAsync(ComputerInfoComparisonResult comparison)
        {
            if (comparison == null) return;
            comparison.UpdateSerialNumber = true;
            comparison.UpdateAssetTag = true;
            comparison.UpdateDomainName = true;
            comparison.UpdateWorkgroup = true;
            comparison.UpdateIsDomainJoined = true;
            comparison.UpdateManufacturer = true;
            comparison.UpdateModel = true;
            comparison.UpdateCpuInfo = true;
            comparison.UpdateTotalRamGb = true;
            comparison.UpdateRamType = true;
            comparison.UpdateStorageInfo = true;
            comparison.UpdateBiosVersion = true;
            comparison.UpdateOsName = true;
            comparison.UpdateOsVersion = true;
            comparison.UpdateOsArchitecture = true;
            comparison.UpdatePrimaryIp = true;
            comparison.UpdatePrimaryMac = true;
            comparison.UpdateSecondaryIps = true;
            comparison.UpdateSecondaryMacs = true;
            comparison.UpdateDnsServers = true;
            comparison.UpdateDefaultGateways = true;
            comparison.UpdateSubnetMasks = true;
            await UpdateSelectedFieldsAsync(comparison);
        }
        public ObservableCollection<ComputerInfoComparisonResult> ComparisonResults
        {
            get => _comparisonResults;
            set
            {
                _comparisonResults = value;
                OnPropertyChanged();
            }
        }

        public string TargetInput
        {
            get => _targetInput;
            set
            {
                _targetInput = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ComputerInfo> ComputerResults
        {
            get => _computerResults;
            set
            {
                _computerResults = value;
                OnPropertyChanged();
            }
        }

        public string Results
        {
            get => _results;
            set
            {
                _results = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                _isScanning = value;
                OnPropertyChanged();
            }
        }

        public bool HasResults
        {
            get => _hasResults;
            set
            {
                _hasResults = value;
                OnPropertyChanged();
            }
        }

        public ICommand ScanCommand { get; }
        public ICommand ClearCommand { get; }


        private async Task ScanComputersAsync()
        {
            try
            {
                IsScanning = true;
                StatusMessage = "Preparing scan...";
                HasResults = false;

                // Determine targets
                List<string> targets;
                if (string.IsNullOrWhiteSpace(TargetInput))
                {
                    targets = new List<string> { Environment.MachineName };
                    StatusMessage = $"Scanning local computer: {Environment.MachineName}";
                }
                else
                {
                    targets = TargetInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(t => t.Trim())
                                        .Where(t => !string.IsNullOrWhiteSpace(t))
                                        .ToList();

                    if (targets.Count == 0)
                    {
                        targets = new List<string> { Environment.MachineName };
                        StatusMessage = $"No valid targets entered. Using local computer: {Environment.MachineName}";
                    }
                    else
                    {
                        StatusMessage = $"Scanning {targets.Count} target(s): {string.Join(", ", targets)}";
                    }
                }

                // Give UI time to update
                await Task.Delay(500);

                // Clear previous results
                ComputerResults.Clear();
                ComparisonResults.Clear();

                // Process each target
                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    StatusMessage = $"Processing {target} ({i + 1}/{targets.Count})...";
                    await Task.Delay(100);

                    var computerInfo = await ProcessTargetAsync(target);
                    ComputerResults.Add(computerInfo);

                    // Lookup in database for comparison
                    DeviceModel? dbDevice = null;
                    try
                    {
                        dbDevice = await _databaseService.GetDeviceByHostnameAsync(target);
                    }
                    catch (Exception)
                    {
                        // Optionally log or handle DB errors
                    }
                    var comparison = new ComputerInfoComparisonResult(computerInfo, dbDevice);
                    ComparisonResults.Add(comparison);
                }

                HasResults = ComputerResults.Count > 0;
                StatusMessage = "Scan completed successfully!";

                // Clear status after a delay
                await Task.Delay(2000);
                if (!IsScanning) // Only clear if not scanning again
                {
                    StatusMessage = "";
                }
            }
            catch (Exception ex)
            {
                ComputerResults.Add(new ComputerInfo
                {
                    ComputerName = "Error",
                    Manufacturer = ex.Message,
                    Model = "Scan Failed",
                    SerialNumber = "N/A",
                    OperatingSystem = "N/A",
                    OSArchitecture = "N/A",
                    BIOSVersion = "N/A",
                    Uptime = "N/A",
                    AssetTag = "N/A",
                    NetworkInfo = "Error occurred during scanning"
                });
                HasResults = true;
                StatusMessage = "Scan failed";
            }
            finally
            {
                IsScanning = false;
            }
        }

        private void ClearResults()
        {
            Results = string.Empty;
            ComputerResults.Clear();
            HasResults = false;
            StatusMessage = "";
        }

        private async Task<ComputerInfo> ProcessTargetAsync(string target)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var computerInfo = new ComputerInfo { ComputerName = target };

                    // Determine if this is a remote or local query
                    bool isRemote = !string.Equals(target, Environment.MachineName, StringComparison.OrdinalIgnoreCase) &&
                                   !string.Equals(target, "localhost", StringComparison.OrdinalIgnoreCase) &&
                                   !target.Equals("127.0.0.1");

                    if (isRemote)
                    {
                        GetRemoteComputerInfo(computerInfo, target);
                    }
                    else
                    {
                        GetLocalComputerInfo(computerInfo);
                    }

                    return computerInfo;
                }
                catch (Exception ex)
                {
                    return new ComputerInfo
                    {
                        ComputerName = target,
                        Manufacturer = $"Error: {ex.Message}",
                        Model = "Unable to process",
                        SerialNumber = "N/A",
                        OperatingSystem = "N/A",
                        OSArchitecture = "N/A",
                        BIOSVersion = "N/A",
                        Uptime = "N/A",
                        AssetTag = "N/A",
                        NetworkInfo = "N/A"
                    };
                }
            });
        }

        // Format computer info for display
        private List<string> FormatComputerInfo(ComputerInfo computerInfo)
        {
            var lines = new List<string>();
            lines.Add($"Computer: {computerInfo.ComputerName}");
            lines.Add($"Manufacturer: {computerInfo.Manufacturer}");
            lines.Add($"Model: {computerInfo.Model}");
            lines.Add($"Serial Number: {computerInfo.SerialNumber}");
            lines.Add($"OS: {computerInfo.OperatingSystem}");
            lines.Add($"OS Architecture: {computerInfo.OSArchitecture}");
            lines.Add($"BIOS Version: {computerInfo.BIOSVersion}");
            lines.Add($"Uptime: {computerInfo.Uptime}");
            lines.Add($"Asset Tag: {computerInfo.AssetTag}");
            lines.Add("");
            lines.Add("Network Information:");
            lines.Add(computerInfo.NetworkInfo);
            return lines;
        }

        private void GetLocalComputerInfo(ComputerInfo computerInfo)
        {
            // Get basic system info
            computerInfo.Manufacturer = GetWmiProperty("Win32_ComputerSystem", "Manufacturer");
            computerInfo.Model = GetWmiProperty("Win32_ComputerSystem", "Model");
            computerInfo.SerialNumber = GetWmiProperty("Win32_BIOS", "SerialNumber");
            computerInfo.OperatingSystem = GetWmiProperty("Win32_OperatingSystem", "Caption");
            computerInfo.OSArchitecture = GetWmiProperty("Win32_OperatingSystem", "OSArchitecture");
            computerInfo.BIOSVersion = GetWmiProperty("Win32_BIOS", "SMBIOSBIOSVersion");
            computerInfo.ProcessorName = GetProcessorName();

            // Get domain/workgroup info
            PopulateDomainInfo(computerInfo);

            // Get memory info
            PopulateMemoryInfo(computerInfo);

            // Get drive info
            PopulateDriveInfo(computerInfo);

            // Get network info
            PopulateNetworkInfo(computerInfo);

            // Get network configuration (DNS, Gateway, Subnet)
            PopulateNetworkConfiguration(computerInfo);

            // Get uptime
            computerInfo.Uptime = GetUptime();

            // Get asset tag (from registry or WMI)
            computerInfo.AssetTag = GetAssetTag();
        }

        private void GetRemoteComputerInfo(ComputerInfo computerInfo, string target)
        {
            try
            {
                // Get basic system info from remote computer
                computerInfo.Manufacturer = GetRemoteWmiProperty(target, "Win32_ComputerSystem", "Manufacturer");
                computerInfo.Model = GetRemoteWmiProperty(target, "Win32_ComputerSystem", "Model");
                computerInfo.SerialNumber = GetRemoteWmiProperty(target, "Win32_BIOS", "SerialNumber");
                computerInfo.OperatingSystem = GetRemoteWmiProperty(target, "Win32_OperatingSystem", "Caption");
                computerInfo.OSVersion = GetRemoteWmiProperty(target, "Win32_OperatingSystem", "Version");
                computerInfo.OSArchitecture = GetRemoteWmiProperty(target, "Win32_OperatingSystem", "OSArchitecture");
                computerInfo.BIOSVersion = GetRemoteWmiProperty(target, "Win32_BIOS", "SMBIOSBIOSVersion");
                computerInfo.ProcessorName = GetRemoteWmiProperty(target, "Win32_Processor", "Name");

                // Get remote domain/workgroup info
                PopulateRemoteDomainInfo(computerInfo, target);

                // Get remote memory info
                PopulateRemoteMemoryInfo(computerInfo, target);

                // Get remote drive info
                PopulateRemoteDriveInfo(computerInfo, target);

                // Get remote uptime
                computerInfo.Uptime = GetRemoteUptime(target);

                // Get remote asset tag
                computerInfo.AssetTag = GetRemoteWmiProperty(target, "Win32_SystemEnclosure", "SMBIOSAssetTag");

                // Get remote network info
                PopulateRemoteNetworkInfo(computerInfo, target);

                // Get remote network configuration (DNS, Gateway, Subnet)
                PopulateRemoteNetworkConfiguration(computerInfo, target);
            }
            catch (Exception ex)
            {
                computerInfo.Manufacturer = $"Error: {ex.Message}";
                computerInfo.Model = "Unable to connect";
                computerInfo.SerialNumber = "N/A";
                computerInfo.OperatingSystem = "N/A";
                computerInfo.OSArchitecture = "N/A";
                computerInfo.BIOSVersion = "N/A";
                computerInfo.ProcessorName = "N/A";
                computerInfo.Uptime = "N/A";
                computerInfo.AssetTag = "N/A";
                computerInfo.NetworkInfo = "Unable to retrieve network information";
                computerInfo.RAMInstalled = "N/A";
                computerInfo.RAMUsed = "N/A";
                computerInfo.RAMUsedPercent = "N/A";
                computerInfo.RAMType = "N/A";
            }
        }

        // Query WMI for a specific property (local computer)
        [SupportedOSPlatform("windows")]
        private string GetWmiProperty(string wmiClass, string propertyName)
        {
            try
            {
                var query = new ObjectQuery($"SELECT * FROM {wmiClass}");
                var searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj[propertyName]?.ToString() ?? "N/A";
                }
            }
            catch
            {
                return "N/A";
            }

            return "N/A";
        }

        // Query WMI for a specific property (remote computer)
        [SupportedOSPlatform("windows")]
        private string GetRemoteWmiProperty(string computerName, string wmiClass, string propertyName)
        {
            try
            {
                var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
                scope.Connect();

                var query = new ObjectQuery($"SELECT * FROM {wmiClass}");
                var searcher = new ManagementObjectSearcher(scope, query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj[propertyName]?.ToString() ?? "N/A";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }

            return "N/A";
        }

        // Get network adapter details (local computer)
        [SupportedOSPlatform("windows")]
        private void PopulateNetworkInfo(ComputerInfo computerInfo)
        {
            try
            {
                computerInfo.NetworkAdapters.Clear();
                var networkInfo = new List<string>(); // Keep old format for fallback

                // Get network interfaces
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(i => i.OperationalStatus == OperationalStatus.Up &&
                               i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                foreach (var iface in interfaces)
                {
                    var adapter = new NetworkAdapter
                    {
                        Name = iface.Name
                    };

                    // Get MAC address
                    var physicalAddress = iface.GetPhysicalAddress();
                    if (physicalAddress != null)
                    {
                        adapter.MACAddress = physicalAddress.ToString();
                    }

                    // Get IP addresses for this interface
                    try
                    {
                        var properties = iface.GetIPProperties();
                        var ipAddresses = new List<string>();

                        // Get unicast addresses
                        foreach (var unicastAddress in properties.UnicastAddresses)
                        {
                            var addr = unicastAddress.Address;
                            
                            // Skip loopback and link-local addresses
                            if (addr.IsIPv6LinkLocal || 
                                addr.Equals(SystemNet.IPAddress.Loopback) || 
                                addr.Equals(SystemNet.IPAddress.IPv6Loopback))
                                continue;

                            // Include IPv4 and IPv6 addresses
                            if (addr.AddressFamily == SystemNet.Sockets.AddressFamily.InterNetwork ||
                                addr.AddressFamily == SystemNet.Sockets.AddressFamily.InterNetworkV6)
                            {
                                ipAddresses.Add(addr.ToString());
                            }
                        }

                        if (ipAddresses.Any())
                        {
                            adapter.IPAddress = string.Join(", ", ipAddresses);
                        }
                    }
                    catch (Exception ex)
                    {
                        // If we can't get IP addresses for this adapter, still include it
                        adapter.IPAddress = $"Error: {ex.Message}";
                    }

                    // Always add the adapter if it has a name (we made HasValidData more lenient)
                    if (adapter.HasValidData)
                    {
                        computerInfo.NetworkAdapters.Add(adapter);
                    }
                }

                // Create fallback string format
                networkInfo.Add("IP Addresses:");
                try
                {
                    var hostName = SystemNet.Dns.GetHostName();
                    var hostEntry = SystemNet.Dns.GetHostEntry(hostName);
                    foreach (var ip in hostEntry.AddressList)
                    {
                        if (ip.AddressFamily == SystemNet.Sockets.AddressFamily.InterNetwork ||
                            ip.AddressFamily == SystemNet.Sockets.AddressFamily.InterNetworkV6)
                        {
                            networkInfo.Add($"  {ip}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    networkInfo.Add($"  Error getting host IPs: {ex.Message}");
                }

                networkInfo.Add("\nMAC Addresses:");
                foreach (var adapter in computerInfo.NetworkAdapters)
                {
                    networkInfo.Add($"  {adapter.Name}: {adapter.FormattedMACAddress}");
                }

                computerInfo.NetworkInfo = string.Join("\n", networkInfo);
            }
            catch (Exception ex)
            {
                computerInfo.NetworkInfo = $"Error getting network info: {ex.Message}";
                computerInfo.NetworkAdapters.Clear();
            }
        }

        // Get network info for remote computer
        [SupportedOSPlatform("windows")]
        private void PopulateRemoteNetworkInfo(ComputerInfo computerInfo, string computerName)
        {
            try
            {
                computerInfo.NetworkAdapters.Clear();
                var networkInfo = new List<string>(); // Keep old format for fallback
                var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
                scope.Connect();

                // Get network adapter configurations
                var query = new ObjectQuery("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true");
                var searcher = new ManagementObjectSearcher(scope, query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    var adapter = new NetworkAdapter();
                    
                    // Get adapter name
                    var description = obj["Description"]?.ToString() ?? "Unknown Adapter";
                    adapter.Name = description;

                    // Get IP addresses
                    var ipAddresses = obj["IPAddress"] as string[];
                    if (ipAddresses != null && ipAddresses.Length > 0)
                    {
                        adapter.IPAddress = string.Join(", ", ipAddresses);
                    }

                    // Get MAC address
                    var macAddress = obj["MACAddress"]?.ToString();
                    if (!string.IsNullOrEmpty(macAddress))
                    {
                        adapter.MACAddress = macAddress.Replace(":", ""); // Remove colons if present
                    }

                    if (adapter.HasValidData)
                    {
                        computerInfo.NetworkAdapters.Add(adapter);
                    }
                }

                // Create fallback string format
                networkInfo.Add("IP Addresses:");
                foreach (var adapter in computerInfo.NetworkAdapters)
                {
                    if (!string.IsNullOrEmpty(adapter.IPAddress))
                    {
                        networkInfo.Add($"  {adapter.IPAddress}");
                    }
                }

                networkInfo.Add("\nMAC Addresses:");
                foreach (var adapter in computerInfo.NetworkAdapters)
                {
                    if (!string.IsNullOrEmpty(adapter.MACAddress))
                    {
                        networkInfo.Add($"  {adapter.Name}: {adapter.FormattedMACAddress}");
                    }
                }

                computerInfo.NetworkInfo = string.Join("\n", networkInfo);
            }
            catch (Exception ex)
            {
                computerInfo.NetworkInfo = $"Error getting remote network info: {ex.Message}";
                computerInfo.NetworkAdapters.Clear();
            }
        }

        // Get system uptime (local computer)
        [SupportedOSPlatform("windows")]
        private string GetUptime()
        {
            try
            {
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // Get system uptime (remote computer)
        [SupportedOSPlatform("windows")]
        private string GetRemoteUptime(string computerName)
        {
            try
            {
                var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
                scope.Connect();

                var query = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                var searcher = new ManagementObjectSearcher(scope, query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    var lastBootUpTime = obj["LastBootUpTime"]?.ToString();
                    if (DateTime.TryParseExact(lastBootUpTime?.Substring(0, 14), "yyyyMMddHHmmss",
                                             null, global::System.Globalization.DateTimeStyles.None, out var bootTime))
                    {
                        var uptime = DateTime.Now - bootTime;
                        return $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }

            return "N/A";
        }

        // Get asset tag from registry or WMI (local computer)
        [SupportedOSPlatform("windows")]
        private string GetAssetTag()
        {
            try
            {
                // Try registry first
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VWG\Inventory");
                var assetTag = key?.GetValue("AssetTag")?.ToString();

                if (!string.IsNullOrEmpty(assetTag) && assetTag != "System SKU")
                {
                    return assetTag;
                }

                // Try WMI as fallback
                return GetWmiProperty("Win32_SystemEnclosure", "SMBIOSAssetTag");
            }
            catch
            {
                return "N/A";
            }
        }

        // Get processor name from registry (local computer)
        [SupportedOSPlatform("windows")]
        private string GetProcessorName()
        {
            try
            {
                // Try registry first (as requested)
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                var processorName = key?.GetValue("ProcessorNameString")?.ToString();

                if (!string.IsNullOrEmpty(processorName))
                {
                    return processorName.Trim();
                }

                // Try WMI as fallback
                return GetWmiProperty("Win32_Processor", "Name");
            }
            catch
            {
                return "N/A";
            }
        }

        // Get memory information (local computer)
        [SupportedOSPlatform("windows")]
        private void PopulateMemoryInfo(ComputerInfo computerInfo)
        {
            try
            {
                // Get total physical memory
                var totalMemoryBytes = GetTotalPhysicalMemory();
                if (totalMemoryBytes > 0)
                {
                    var totalMemoryGB = totalMemoryBytes / (1000.0 * 1000.0 * 1000.0);
                    computerInfo.RAMInstalled = $"{totalMemoryGB:F1} GB";
                    computerInfo.RAMInstalledGB = totalMemoryGB;

                    // Get memory usage using performance counter or WMI
                    var availableMemoryBytes = GetAvailableMemory();
                    if (availableMemoryBytes > 0)
                    {
                        var usedMemoryBytes = totalMemoryBytes - availableMemoryBytes;
                        var usedMemoryGB = usedMemoryBytes / (1000.0 * 1000.0 * 1000.0);
                        var usedPercent = (double)usedMemoryBytes / totalMemoryBytes * 100;

                        computerInfo.RAMUsed = $"{usedMemoryGB:F1} GB";
                        computerInfo.RAMUsedGB = usedMemoryGB;
                        computerInfo.RAMUsedPercent = $"{usedPercent:F1}%";
                    }
                    else
                    {
                        // Available memory calculation failed, set defaults
                        computerInfo.RAMUsed = "N/A";
                        computerInfo.RAMUsedGB = 0;
                        computerInfo.RAMUsedPercent = "N/A";
                    }
                }

                // Get RAM type
                computerInfo.RAMType = GetRAMType();
            }
            catch (Exception ex)
            {
                computerInfo.RAMInstalled = $"Error: {ex.Message}";
                computerInfo.RAMUsed = "N/A";
                computerInfo.RAMUsedPercent = "N/A";
                computerInfo.RAMType = "N/A";
                computerInfo.RAMInstalledGB = 0;
                computerInfo.RAMUsedGB = 0;
            }
        }

        // Get domain/workgroup information (local computer)
        private void PopulateDomainInfo(ComputerInfo computerInfo)
        {
            try
            {
                computerInfo.Domain = GetWmiProperty("Win32_ComputerSystem", "Domain") ?? "";
                computerInfo.Workgroup = GetWmiProperty("Win32_ComputerSystem", "Workgroup") ?? "";
                var partOfDomain = GetWmiProperty("Win32_ComputerSystem", "PartOfDomain");
                computerInfo.IsDomainJoined = string.Equals(partOfDomain, "True", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                computerInfo.Domain = $"Error: {ex.Message}";
                computerInfo.Workgroup = "N/A";
                computerInfo.IsDomainJoined = false;
            }
        }

        // Get network configuration (DNS, Gateway, Subnet) for local computer
        private void PopulateNetworkConfiguration(ComputerInfo computerInfo)
        {
            try
            {
                computerInfo.DNSServers.Clear();
                computerInfo.DefaultGateways.Clear();
                computerInfo.SubnetMasks.Clear();

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // Get DNS servers
                        var dnsServers = obj["DNSServerSearchOrder"] as string[];
                        if (dnsServers != null)
                        {
                            foreach (var dns in dnsServers.Where(d => !string.IsNullOrEmpty(d)))
                            {
                                if (!computerInfo.DNSServers.Contains(dns))
                                    computerInfo.DNSServers.Add(dns);
                            }
                        }

                        // Get default gateways
                        var gateways = obj["DefaultIPGateway"] as string[];
                        if (gateways != null)
                        {
                            foreach (var gateway in gateways.Where(g => !string.IsNullOrEmpty(g)))
                            {
                                if (!computerInfo.DefaultGateways.Contains(gateway))
                                    computerInfo.DefaultGateways.Add(gateway);
                            }
                        }

                        // Get subnet masks
                        var subnets = obj["IPSubnet"] as string[];
                        if (subnets != null)
                        {
                            foreach (var subnet in subnets.Where(s => !string.IsNullOrEmpty(s)))
                            {
                                if (!computerInfo.SubnetMasks.Contains(subnet))
                                    computerInfo.SubnetMasks.Add(subnet);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                computerInfo.DNSServers.Clear();
                computerInfo.DefaultGateways.Clear();
                computerInfo.SubnetMasks.Clear();
                // Add error indicator
                computerInfo.DNSServers.Add($"Error: {ex.Message}");
            }
        }

        // Get drive information (local computer)
        private void PopulateDriveInfo(ComputerInfo computerInfo)
        {
            try
            {
                computerInfo.DriveDetails.Clear();
                var drives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == System.IO.DriveType.Fixed)
                    .OrderBy(d => d.Name)
                    .ToList();

                foreach (var drive in drives)
                {
                    try
                    {
                        var totalGB = drive.TotalSize / (1000L * 1000L * 1000L);
                        var freeGB = drive.AvailableFreeSpace / (1000L * 1000L * 1000L);
                        var usedGB = totalGB - freeGB;
                        var usedPercent = totalGB > 0 ? (double)usedGB / totalGB * 100 : 0;

                        var driveInfo = new DriveDetails
                        {
                            DriveLetter = drive.Name,
                            DriveType = drive.DriveType.ToString(),
                            FileSystem = drive.DriveFormat,
                            TotalSizeGB = totalGB,
                            UsedSizeGB = usedGB,
                            FreeSizeGB = freeGB,
                            UsedPercent = usedPercent
                        };

                        computerInfo.DriveDetails.Add(driveInfo);
                    }
                    catch (Exception ex)
                    {
                        // Add error entry for this drive
                        var driveInfo = new DriveDetails
                        {
                            DriveLetter = drive.Name,
                            DriveType = "Error",
                            FileSystem = ex.Message,
                            TotalSizeGB = 0,
                            UsedSizeGB = 0,
                            FreeSizeGB = 0,
                            UsedPercent = 0
                        };
                        computerInfo.DriveDetails.Add(driveInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                // Add general error entry
                var errorDrive = new DriveDetails
                {
                    DriveLetter = "Error",
                    DriveType = "Error",
                    FileSystem = ex.Message,
                    TotalSizeGB = 0,
                    UsedSizeGB = 0,
                    FreeSizeGB = 0,
                    UsedPercent = 0
                };
                computerInfo.DriveDetails.Add(errorDrive);
            }
        }

        // Helper methods for memory information
        [SupportedOSPlatform("windows")]
        private long GetTotalPhysicalMemory()
        {
            try
            {
                // Method 1: Try Performance Counter for total visible memory
                using var totalCounter = new System.Diagnostics.PerformanceCounter("Memory", "Total Visible Memory Size");
                var totalBytes = (long)totalCounter.NextValue();
                if (totalBytes > 0)
                {
                    // Counter returns in KB, convert to bytes
                    return totalBytes * 1024;
                }
            }
            catch { }

            try
            {
                // Method 2: Fallback to WMI
                var query = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
                var searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    var totalMemory = obj["TotalPhysicalMemory"]?.ToString();
                    if (long.TryParse(totalMemory, out var bytes))
                    {
                        return bytes;
                    }
                }
            }
            catch { }
            return 0;
        }

        [SupportedOSPlatform("Windows")]
        private long GetAvailableMemory()
        {
            try
            {
                // Method 1: Try Performance Counter (most reliable, like Task Manager)
                using var availableCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available Bytes");
                var availableBytes = (long)availableCounter.NextValue();
                if (availableBytes > 0)
                {
                    return availableBytes;
                }
            }
            catch { }

            try
            {
                // Method 2: Fallback to WMI Win32_PerfRawData_PerfOS_Memory
                var query = new ObjectQuery("SELECT * FROM Win32_PerfRawData_PerfOS_Memory");
                var searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    var availableBytes = obj["AvailableBytes"]?.ToString();
                    if (long.TryParse(availableBytes, out var bytes))
                    {
                        return bytes;
                    }
                }
            }
            catch { }

            try
            {
                // Method 3: Fallback to original WMI method
                var query = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                var searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    var availableMemory = obj["AvailablePhysicalMemory"]?.ToString();
                    if (long.TryParse(availableMemory, out var kbytes))
                    {
                        return kbytes * 1024; // Convert from KB to bytes
                    }
                }
            }
            catch { }
            
            return 0;
        }

        [SupportedOSPlatform("windows")]
        private string GetRAMType()
        {
            try
            {
                var query = new ObjectQuery("SELECT * FROM Win32_PhysicalMemory");
                var searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    var memoryType = obj["MemoryType"]?.ToString();
                    var smbiosMemoryType = obj["SMBIOSMemoryType"]?.ToString();
                    
                    // Try to determine RAM type from SMBIOS first
                    if (int.TryParse(smbiosMemoryType, out var smbiosType))
                    {
                        return smbiosType switch
                        {
                            26 => "DDR4",
                            24 => "DDR3",
                            21 => "DDR2",
                            20 => "DDR",
                            _ => $"Type {smbiosType}"
                        };
                    }
                    
                    // Fallback to MemoryType
                    if (int.TryParse(memoryType, out var type))
                    {
                        return type switch
                        {
                            0 => "Unknown",
                            1 => "Other",
                            2 => "DRAM",
                            3 => "Synchronous DRAM",
                            4 => "Cache DRAM",
                            5 => "EDO",
                            6 => "EDRAM",
                            7 => "VRAM",
                            8 => "SRAM",
                            9 => "RAM",
                            10 => "ROM",
                            11 => "Flash",
                            12 => "EEPROM",
                            13 => "FEPROM",
                            14 => "EPROM",
                            15 => "CDRAM",
                            16 => "3DRAM",
                            17 => "SDRAM",
                            18 => "SGRAM",
                            19 => "RDRAM",
                            20 => "DDR",
                            21 => "DDR2",
                            22 => "DDR2 FB-DIMM",
                            24 => "DDR3",
                            25 => "FBD2",
                            26 => "DDR4",
                            _ => $"Type {type}"
                        };
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        // Get memory information (remote computer)
        [SupportedOSPlatform("windows")]
        private void PopulateRemoteMemoryInfo(ComputerInfo computerInfo, string computerName)
        {
            try
            {
                var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
                scope.Connect();

                // Get total physical memory
                var query = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
                var searcher = new ManagementObjectSearcher(scope, query);

                long totalMemoryBytes = 0;
                foreach (ManagementObject obj in searcher.Get())
                {
                    var totalMemory = obj["TotalPhysicalMemory"]?.ToString();
                    if (long.TryParse(totalMemory, out totalMemoryBytes))
                    {
                        var totalMemoryGB = totalMemoryBytes / (1000.0 * 1000.0 * 1000.0);
                        computerInfo.RAMInstalled = $"{totalMemoryGB:F1} GB";
                        computerInfo.RAMInstalledGB = totalMemoryGB;
                        break;
                    }
                }

                // Get available memory
                if (totalMemoryBytes > 0)
                {
                    var osQuery = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                    var osSearcher = new ManagementObjectSearcher(scope, osQuery);

                    bool foundAvailable = false;
                    foreach (ManagementObject obj in osSearcher.Get())
                    {
                        var availableMemory = obj["AvailablePhysicalMemory"]?.ToString();
                        if (long.TryParse(availableMemory, out var availableKB))
                        {
                            var availableBytes = availableKB * 1024;
                            var usedMemoryBytes = totalMemoryBytes - availableBytes;
                            var usedMemoryGB = usedMemoryBytes / (1000.0 * 1000.0 * 1000.0);
                            var usedPercent = (double)usedMemoryBytes / totalMemoryBytes * 100;

                            computerInfo.RAMUsed = $"{usedMemoryGB:F1} GB";
                            computerInfo.RAMUsedGB = usedMemoryGB;
                            computerInfo.RAMUsedPercent = $"{usedPercent:F1}%";
                            foundAvailable = true;
                            break;
                        }
                    }
                    
                    if (!foundAvailable)
                    {
                        // Available memory calculation failed, set defaults
                        computerInfo.RAMUsed = "N/A";
                        computerInfo.RAMUsedGB = 0;
                        computerInfo.RAMUsedPercent = "N/A";
                    }
                }

                // Get RAM type
                computerInfo.RAMType = GetRemoteRAMType(computerName);
            }
            catch (Exception ex)
            {
                computerInfo.RAMInstalled = $"Error: {ex.Message}";
                computerInfo.RAMUsed = "N/A";
                computerInfo.RAMUsedPercent = "N/A";
                computerInfo.RAMType = "N/A";
                computerInfo.RAMInstalledGB = 0;
                computerInfo.RAMUsedGB = 0;
            }
        }

        // Get domain/workgroup information (remote computer)
        private void PopulateRemoteDomainInfo(ComputerInfo computerInfo, string computerName)
        {
            try
            {
                computerInfo.Domain = GetRemoteWmiProperty(computerName, "Win32_ComputerSystem", "Domain") ?? "";
                computerInfo.Workgroup = GetRemoteWmiProperty(computerName, "Win32_ComputerSystem", "Workgroup") ?? "";
                var partOfDomain = GetRemoteWmiProperty(computerName, "Win32_ComputerSystem", "PartOfDomain");
                computerInfo.IsDomainJoined = string.Equals(partOfDomain, "True", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                computerInfo.Domain = $"Error: {ex.Message}";
                computerInfo.Workgroup = "N/A";
                computerInfo.IsDomainJoined = false;
            }
        }

        // Get network configuration (DNS, Gateway, Subnet) for remote computer
        private void PopulateRemoteNetworkConfiguration(ComputerInfo computerInfo, string computerName)
        {
            try
            {
                computerInfo.DNSServers.Clear();
                computerInfo.DefaultGateways.Clear();
                computerInfo.SubnetMasks.Clear();

                var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
                scope.Connect();

                using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE")))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // Get DNS servers
                        var dnsServers = obj["DNSServerSearchOrder"] as string[];
                        if (dnsServers != null)
                        {
                            foreach (var dns in dnsServers.Where(d => !string.IsNullOrEmpty(d)))
                            {
                                if (!computerInfo.DNSServers.Contains(dns))
                                    computerInfo.DNSServers.Add(dns);
                            }
                        }

                        // Get default gateways
                        var gateways = obj["DefaultIPGateway"] as string[];
                        if (gateways != null)
                        {
                            foreach (var gateway in gateways.Where(g => !string.IsNullOrEmpty(g)))
                            {
                                if (!computerInfo.DefaultGateways.Contains(gateway))
                                    computerInfo.DefaultGateways.Add(gateway);
                            }
                        }

                        // Get subnet masks
                        var subnets = obj["IPSubnet"] as string[];
                        if (subnets != null)
                        {
                            foreach (var subnet in subnets.Where(s => !string.IsNullOrEmpty(s)))
                            {
                                if (!computerInfo.SubnetMasks.Contains(subnet))
                                    computerInfo.SubnetMasks.Add(subnet);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                computerInfo.DNSServers.Clear();
                computerInfo.DefaultGateways.Clear();
                computerInfo.SubnetMasks.Clear();
                // Add error indicator
                computerInfo.DNSServers.Add($"Error: {ex.Message}");
            }
        }

        // Get drive information (remote computer)
        [SupportedOSPlatform("windows")]
        private void PopulateRemoteDriveInfo(ComputerInfo computerInfo, string computerName)
        {
            try
            {
                computerInfo.DriveDetails.Clear();
                var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
                scope.Connect();

                var query = new ObjectQuery("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3"); // Fixed drives only
                var searcher = new ManagementObjectSearcher(scope, query);

                var drives = new List<DriveDetails>();
                foreach (ManagementObject obj in searcher.Get())
                {
                    try
                    {
                        var driveLetter = obj["DeviceID"]?.ToString() ?? "Unknown";
                        var fileSystem = obj["FileSystem"]?.ToString() ?? "Unknown";
                        var sizeStr = obj["Size"]?.ToString();
                        var freeSpaceStr = obj["FreeSpace"]?.ToString();

                        if (long.TryParse(sizeStr, out var sizeBytes) && 
                            long.TryParse(freeSpaceStr, out var freeBytes))
                        {
                            var totalGB = sizeBytes / (1000L * 1000L * 1000L);
                            var freeGB = freeBytes / (1000L * 1000L * 1000L);
                            var usedGB = totalGB - freeGB;
                            var usedPercent = totalGB > 0 ? (double)usedGB / totalGB * 100 : 0;

                            var driveInfo = new DriveDetails
                            {
                                DriveLetter = driveLetter,
                                DriveType = "Fixed",
                                FileSystem = fileSystem,
                                TotalSizeGB = totalGB,
                                UsedSizeGB = usedGB,
                                FreeSizeGB = freeGB,
                                UsedPercent = usedPercent
                            };

                            drives.Add(driveInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorDrive = new DriveDetails
                        {
                            DriveLetter = obj["DeviceID"]?.ToString() ?? "Unknown",
                            DriveType = "Error",
                            FileSystem = ex.Message,
                            TotalSizeGB = 0,
                            UsedSizeGB = 0,
                            FreeSizeGB = 0,
                            UsedPercent = 0
                        };
                        drives.Add(errorDrive);
                    }
                }

                // Sort drives by letter
                computerInfo.DriveDetails.AddRange(drives.OrderBy(d => d.DriveLetter));
            }
            catch (Exception ex)
            {
                var errorDrive = new DriveDetails
                {
                    DriveLetter = "Error",
                    DriveType = "Error",
                    FileSystem = ex.Message,
                    TotalSizeGB = 0,
                    UsedSizeGB = 0,
                    FreeSizeGB = 0,
                    UsedPercent = 0
                };
                computerInfo.DriveDetails.Add(errorDrive);
            }
        }

        [SupportedOSPlatform("windows")]
        private string GetRemoteRAMType(string computerName)
        {
            try
            {
                var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
                scope.Connect();

                var query = new ObjectQuery("SELECT * FROM Win32_PhysicalMemory");
                var searcher = new ManagementObjectSearcher(scope, query);

                foreach (ManagementObject obj in searcher.Get())
                {
                    var smbiosMemoryType = obj["SMBIOSMemoryType"]?.ToString();
                    var memoryType = obj["MemoryType"]?.ToString();
                    
                    // Try to determine RAM type from SMBIOS first
                    if (int.TryParse(smbiosMemoryType, out var smbiosType))
                    {
                        return smbiosType switch
                        {
                            26 => "DDR4",
                            24 => "DDR3",
                            21 => "DDR2",
                            20 => "DDR",
                            _ => $"Type {smbiosType}"
                        };
                    }
                    
                    // Fallback to MemoryType
                    if (int.TryParse(memoryType, out var type))
                    {
                        return type switch
                        {
                            20 => "DDR",
                            21 => "DDR2",
                            24 => "DDR3",
                            26 => "DDR4",
                            _ => $"Type {type}"
                        };
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class NetworkAdapter
    {
        public string Name { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string MACAddress { get; set; } = string.Empty;
        
        // Computed properties for UI
        public string FormattedMACAddress 
        { 
            get
            {
                if (string.IsNullOrEmpty(MACAddress))
                    return string.Empty;
                
                // Remove any existing separators
                var cleanMac = MACAddress.Replace(":", "").Replace("-", "").Replace(" ", "");
                
                // If not exactly 12 characters, return as-is
                if (cleanMac.Length != 12)
                    return MACAddress;
                    
                // Convert from format like "001122334455" to "00:11:22:33:44:55"
                return string.Join(":", Enumerable.Range(0, 6)
                    .Select(i => cleanMac.Substring(i * 2, 2)));
            }
        }
        
        public bool HasValidData => !string.IsNullOrEmpty(Name);
        public bool HasIPAddress => !string.IsNullOrEmpty(IPAddress);
        public bool HasMACAddress => !string.IsNullOrEmpty(MACAddress);
    }

    public class DriveDetails
    {
        public string DriveLetter { get; set; } = string.Empty;
        public string DriveType { get; set; } = string.Empty;
        public string FileSystem { get; set; } = string.Empty;
        public long TotalSizeGB { get; set; }
        public long UsedSizeGB { get; set; }
        public long FreeSizeGB { get; set; }
        public double UsedPercent { get; set; }
        
        // Computed properties for UI
        public string DisplayName => DriveLetter;
        public string SizeInfo => $"{UsedSizeGB:N0} GB / {TotalSizeGB:N0} GB ({UsedPercent:F1}% used)";
        public string FreeSpaceInfo => $"{FreeSizeGB:N0} GB free";
    }

    public class ComputerInfo
    {
        public string ComputerName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string OSArchitecture { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public string BIOSVersion { get; set; } = string.Empty;
        public string ProcessorName { get; set; } = string.Empty;
        public string Uptime { get; set; } = string.Empty;
        public string AssetTag { get; set; } = string.Empty;
        public string NetworkInfo { get; set; } = string.Empty; // Keep for backwards compatibility
        public List<NetworkAdapter> NetworkAdapters { get; set; } = new();
        
        // New network configuration properties
        public string Domain { get; set; } = string.Empty;
        public string Workgroup { get; set; } = string.Empty;
        public bool IsDomainJoined { get; set; }
        public List<string> DNSServers { get; set; } = new();
        public List<string> DefaultGateways { get; set; } = new();
        public List<string> SubnetMasks { get; set; } = new();
        
        // Memory Information
        public string RAMType { get; set; } = string.Empty;
        public string RAMInstalled { get; set; } = string.Empty;
        public string RAMUsed { get; set; } = string.Empty;
        public string RAMUsedPercent { get; set; } = string.Empty;
        
        // Raw memory values for calculations
        public double RAMInstalledGB { get; set; }
        public double RAMUsedGB { get; set; }
        
        // Computed properties for formatted display
        public string RAMUsageInfo 
        { 
            get
            {
                if (RAMInstalledGB > 0)
                {
                    if (RAMUsedGB > 0)
                    {
                        var usedPercent = (RAMUsedGB / RAMInstalledGB) * 100;
                        return $"{RAMUsedGB:F1} GB / {RAMInstalledGB:F1} GB ({usedPercent:F1}% used)";
                    }
                    else
                    {
                        // Show installed amount even if usage calculation failed
                        return $"Usage unavailable / {RAMInstalledGB:F1} GB installed";
                    }
                }
                
                // Fallback to individual strings if raw values aren't available
                if (!string.IsNullOrEmpty(RAMUsed) && !string.IsNullOrEmpty(RAMInstalled) && 
                    !RAMUsed.Contains("Error") && !RAMInstalled.Contains("Error"))
                {
                    return $"{RAMUsed} / {RAMInstalled}";
                }
                
                return RAMUsed ?? "N/A";
            }
        }
        
        // Drive Information
        public List<DriveDetails> DriveDetails { get; set; } = new();
        
        // Computed properties for UI
        public bool IsError => Manufacturer.StartsWith("Error") || Model == "Scan Failed";
        public string DisplayName => string.IsNullOrEmpty(ComputerName) ? "Unknown Computer" : ComputerName;
        public string HardwareInfo => $"{Manufacturer} {Model}".Trim();
        public bool HasAssetTag => !string.IsNullOrEmpty(AssetTag) && AssetTag != "N/A" && AssetTag != "Unknown";
        public bool HasNetworkAdapters => NetworkAdapters.Any(n => n.HasValidData);
        public bool HasMemoryInfo => !string.IsNullOrEmpty(RAMInstalled) && RAMInstalled != "N/A";
        public bool HasDriveInfo => DriveDetails.Any();
        
        // New computed properties for network configuration display (for UI binding)
        public string DomainOrWorkgroupDisplay
        {
            get
            {
                if (IsDomainJoined && !string.IsNullOrWhiteSpace(Domain))
                    return Domain;
                if (!IsDomainJoined && !string.IsNullOrWhiteSpace(Workgroup))
                    return Workgroup;
                if (!string.IsNullOrWhiteSpace(Domain))
                    return Domain;
                if (!string.IsNullOrWhiteSpace(Workgroup))
                    return Workgroup;
                return "N/A";
            }
        }

    public string DNSServersDisplay => DNSServers != null && DNSServers.Any() ? string.Join("\n", DNSServers) : "N/A";
    public string DefaultGatewaysDisplay => DefaultGateways != null && DefaultGateways.Any() ? string.Join("\n", DefaultGateways) : "N/A";
    public string SubnetMasksDisplay => SubnetMasks != null && SubnetMasks.Any() ? string.Join("\n", SubnetMasks) : "N/A";
        public string IsDomainJoinedDisplay => IsDomainJoined ? "Yes" : "No";
    }
}
