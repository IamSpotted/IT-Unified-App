// Originally written in PowerShell by [Thomas Blake]
// Converted to C# by Jon Uldrick for integration with the MAUI IT Support Framework
// Enhanced to match PowerShell Get-ComputerInfo-v2.0.ps1 functionality

using MauiApp1.Interfaces;
using MauiApp1.Views;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using Microsoft.Win32;
using System.Net;
using System.DirectoryServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace MauiApp1.Scripts
{
    public class GetComputerInfo : IScript
    {
        public string ScriptName => "Get Computer Info";
        public string Description => "Gets comprehensive computer info including hardware, OS, network, memory, and storage details. Supports local and remote computers.";
        public string Category => "System";
        public string Author => "Thomas Blake";

        public async void Execute()
        {
            try
            {
                // Navigate directly to the Computer Info page
                await Shell.Current.GoToAsync(nameof(ComputerInfoPage));
            }
            catch (Exception ex)
            {
                // Fallback to alert if navigation fails
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open Computer Info page: {ex.Message}", "OK");
                }
            }
        }
    }

    /// <summary>
    /// Comprehensive computer information collection service matching PowerShell Get-ComputerInfo-v2.0.ps1 functionality
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ComputerInfoCollectionService
    {
        /// <summary>
        /// Collect comprehensive computer information for the specified target
        /// </summary>
        /// <param name="computerName">Computer name or IP address (empty for local computer)</param>
        /// <param name="includeActiveDirectory">Whether to include Active Directory information</param>
        /// <returns>Detailed computer information</returns>
        public static async Task<EnhancedComputerInfo> GetComputerInfoAsync(string computerName = "", bool includeActiveDirectory = true)
        {
            return await Task.Run(() =>
            {
                var computerInfo = new EnhancedComputerInfo();
                
                try
                {
                    bool isRemote = !string.IsNullOrWhiteSpace(computerName) && 
                                   !string.Equals(computerName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) &&
                                   !string.Equals(computerName, "localhost", StringComparison.OrdinalIgnoreCase) &&
                                   !computerName.Equals("127.0.0.1");

                    computerInfo.ComputerName = isRemote ? computerName : Environment.MachineName;

                    if (isRemote)
                    {
                        CollectRemoteComputerInfo(computerInfo, computerName);
                    }
                    else
                    {
                        CollectLocalComputerInfo(computerInfo);
                    }

                    // Collect Active Directory information if requested
                    if (includeActiveDirectory)
                    {
                        CollectActiveDirectoryInfo(computerInfo);
                    }
                }
                catch (Exception ex)
                {
                    computerInfo.ErrorMessage = $"Error collecting computer info: {ex.Message}";
                }

                return computerInfo;
            });
        }

        private static void CollectLocalComputerInfo(EnhancedComputerInfo computerInfo)
        {
            // Hardware Information
            CollectHardwareInfo(computerInfo);
            
            // Operating System Information
            CollectOperatingSystemInfo(computerInfo);
            
            // Network Information
            CollectNetworkInfo(computerInfo);
            
            // Physical Disk Information
            CollectPhysicalDiskInfo(computerInfo);
            
            // Physical Memory Information
            CollectPhysicalMemoryInfo(computerInfo);
        }

        private static void CollectRemoteComputerInfo(EnhancedComputerInfo computerInfo, string computerName)
        {
            try
            {
                var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
                scope.Connect();

                // Hardware Information
                CollectRemoteHardwareInfo(computerInfo, scope);
                
                // Operating System Information
                CollectRemoteOperatingSystemInfo(computerInfo, scope);
                
                // Network Information
                CollectRemoteNetworkInfo(computerInfo, scope);
                
                // Physical Disk Information
                CollectRemotePhysicalDiskInfo(computerInfo, scope, computerName);
                
                // Physical Memory Information
                CollectRemotePhysicalMemoryInfo(computerInfo, scope);
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage = $"Error connecting to remote computer {computerName}: {ex.Message}";
            }
        }

        #region Hardware Information Collection

        private static void CollectHardwareInfo(EnhancedComputerInfo computerInfo)
        {
            try
            {
                // Computer System
                var computerSystem = GetLocalWmiObjects("Win32_ComputerSystem").FirstOrDefault();
                if (computerSystem != null)
                {
                    computerInfo.Manufacturer = computerSystem["Manufacturer"]?.ToString() ?? "N/A";
                    computerInfo.Model = computerSystem["Model"]?.ToString() ?? "N/A";
                }

                // BIOS Information
                var bios = GetLocalWmiObjects("Win32_BIOS").FirstOrDefault();
                if (bios != null)
                {
                    computerInfo.SerialNumber = bios["SerialNumber"]?.ToString() ?? "N/A";
                    computerInfo.BIOSVersion = bios["SMBIOSBIOSVersion"]?.ToString() ?? "N/A";
                    
                    if (DateTime.TryParse(bios["ReleaseDate"]?.ToString(), out var releaseDate))
                    {
                        computerInfo.BIOSReleaseDate = releaseDate.ToShortDateString();
                        var age = DateTime.Now - releaseDate;
                        var years = (int)(age.Days / 365.25);
                        var remainingDays = age.Days - (int)(years * 365.25);
                        computerInfo.BIOSAge = $"{years} years and {remainingDays} days";
                    }
                }

                // Processor Information
                var processor = GetLocalWmiObjects("Win32_Processor").FirstOrDefault();
                if (processor != null)
                {
                    computerInfo.ProcessorName = processor["Name"]?.ToString() ?? "N/A";
                }

                // Asset Tag
                computerInfo.AssetTag = GetAssetTag();

                // Local Time and Uptime
                computerInfo.LocalTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                computerInfo.Uptime = $"{uptime.Days} Days, {uptime.Hours} Hours, {uptime.Minutes} Minutes, {uptime.Seconds} Seconds";
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"Hardware info error: {ex.Message}; ";
            }
        }

        private static void CollectRemoteHardwareInfo(EnhancedComputerInfo computerInfo, ManagementScope scope)
        {
            try
            {
                // Computer System
                var computerSystem = GetRemoteWmiObjects(scope, "Win32_ComputerSystem").FirstOrDefault();
                if (computerSystem != null)
                {
                    computerInfo.Manufacturer = computerSystem["Manufacturer"]?.ToString() ?? "N/A";
                    computerInfo.Model = computerSystem["Model"]?.ToString() ?? "N/A";
                }

                // BIOS Information
                var bios = GetRemoteWmiObjects(scope, "Win32_BIOS").FirstOrDefault();
                if (bios != null)
                {
                    computerInfo.SerialNumber = bios["SerialNumber"]?.ToString() ?? "N/A";
                    computerInfo.BIOSVersion = bios["SMBIOSBIOSVersion"]?.ToString() ?? "N/A";

                    var releaseDateStr = bios["ReleaseDate"]?.ToString();
                    if (!string.IsNullOrEmpty(releaseDateStr) && releaseDateStr.Length >= 8)
                    {
                        if (DateTime.TryParseExact(releaseDateStr.Substring(0, 8), "yyyyMMdd", null, DateTimeStyles.None, out var releaseDate))
                        {
                            computerInfo.BIOSReleaseDate = releaseDate.ToShortDateString();
                            var age = DateTime.Now - releaseDate;
                            var years = (int)(age.Days / 365.25);
                            var remainingDays = age.Days - (int)(years * 365.25);
                            computerInfo.BIOSAge = $"{years} years and {remainingDays} days";
                        }
                    }
                }

                // Processor Information
                var processor = GetRemoteWmiObjects(scope, "Win32_Processor").FirstOrDefault();
                if (processor != null)
                {
                    computerInfo.ProcessorName = processor["Name"]?.ToString() ?? "N/A";
                }

                // Local Time
                var localTime = GetRemoteWmiObjects(scope, "Win32_LocalTime").FirstOrDefault();
                if (localTime != null)
                {
                    var month = localTime["Month"]?.ToString()?.PadLeft(2, '0') ?? "00";
                    var day = localTime["Day"]?.ToString()?.PadLeft(2, '0') ?? "00";
                    var year = localTime["Year"]?.ToString() ?? "0000";
                    var hour = localTime["Hour"]?.ToString()?.PadLeft(2, '0') ?? "00";
                    var minute = localTime["Minute"]?.ToString()?.PadLeft(2, '0') ?? "00";
                    var second = localTime["Second"]?.ToString()?.PadLeft(2, '0') ?? "00";

                    computerInfo.LocalTime = $"{month}/{day}/{year} {hour}:{minute}:{second}";
                }

                // Uptime
                var os = GetRemoteWmiObjects(scope, "Win32_OperatingSystem").FirstOrDefault();
                if (os != null)
                {
                    var lastBootUpTime = os["LastBootUpTime"]?.ToString();
                    if (!string.IsNullOrEmpty(lastBootUpTime) && lastBootUpTime.Length >= 14)
                    {
                        if (DateTime.TryParseExact(lastBootUpTime.Substring(0, 14), "yyyyMMddHHmmss", null, DateTimeStyles.None, out var bootTime))
                        {
                            var uptime = DateTime.Now - bootTime;
                            computerInfo.Uptime = $"{uptime.Days} Days, {uptime.Hours} Hours, {uptime.Minutes} Minutes, {uptime.Seconds} Seconds";
                        }
                    }
                }

                // Asset Tag from remote registry (equivalent to Invoke-Command approach)
                computerInfo.AssetTag = GetRemoteRegistryValue(scope, @"SOFTWARE\VWG\Inventory", "AssetTag"); 
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"Remote hardware info error: {ex.Message}; ";
            }
        }

        #endregion

        #region Operating System Information Collection

        private static void CollectOperatingSystemInfo(EnhancedComputerInfo computerInfo)
        {
            try
            {
                var os = GetLocalWmiObjects("Win32_OperatingSystem").FirstOrDefault();
                if (os != null)
                {
                    computerInfo.OperatingSystem = os["Caption"]?.ToString() ?? "N/A";
                    computerInfo.OSVersion = os["Version"]?.ToString() ?? "N/A";
                    
                    var installDateStr = os["InstallDate"]?.ToString();
                    if (!string.IsNullOrEmpty(installDateStr) && installDateStr.Length >= 8)
                    {
                        if (DateTime.TryParseExact(installDateStr.Substring(0, 8), "yyyyMMdd", null, DateTimeStyles.None, out var installDate))
                        {
                            computerInfo.OSInstallDate = installDate.ToShortDateString();
                        }
                    }
                }

                var computerSystem = GetLocalWmiObjects("Win32_ComputerSystem").FirstOrDefault();
                if (computerSystem != null)
                {
                    computerInfo.OSArchitecture = computerSystem["SystemType"]?.ToString() ?? "N/A";
                }
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"OS info error: {ex.Message}; ";
            }
        }

        private static void CollectRemoteOperatingSystemInfo(EnhancedComputerInfo computerInfo, ManagementScope scope)
        {
            try
            {
                var os = GetRemoteWmiObjects(scope, "Win32_OperatingSystem").FirstOrDefault();
                if (os != null)
                {
                    computerInfo.OperatingSystem = os["Caption"]?.ToString() ?? "N/A";
                    computerInfo.OSVersion = os["Version"]?.ToString() ?? "N/A";
                    
                    var installDateStr = os["InstallDate"]?.ToString();
                    if (!string.IsNullOrEmpty(installDateStr) && installDateStr.Length >= 8)
                    {
                        if (DateTime.TryParseExact(installDateStr.Substring(0, 8), "yyyyMMdd", null, DateTimeStyles.None, out var installDate))
                        {
                            computerInfo.OSInstallDate = installDate.ToShortDateString();
                        }
                    }
                }

                var computerSystem = GetRemoteWmiObjects(scope, "Win32_ComputerSystem").FirstOrDefault();
                if (computerSystem != null)
                {
                    computerInfo.OSArchitecture = computerSystem["SystemType"]?.ToString() ?? "N/A";
                }
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"Remote OS info error: {ex.Message}; ";
            }
        }

        #endregion

        #region Network Information Collection

        private static void CollectNetworkInfo(EnhancedComputerInfo computerInfo)
        {
            try
            {
                var networkAdapters = new List<EnhancedNetworkAdapter>();

                // Get all physical network adapters (including wireless) with comprehensive filtering
                var physicalAdapters = GetLocalWmiObjects("Win32_NetworkAdapter")
                    .Where(adapter => 
                    {
                        var adapterType = adapter["AdapterType"]?.ToString() ?? "";
                        var description = adapter["Description"]?.ToString() ?? "";
                        var name = adapter["Name"]?.ToString() ?? "";
                        var netConnectionId = adapter["NetConnectionID"]?.ToString() ?? "";
                        var physicalAdapter = adapter["PhysicalAdapter"];
                        var pnpDeviceId = adapter["PNPDeviceID"]?.ToString() ?? "";
                        
                        // More comprehensive filtering
                        // Include if AdapterType contains network-related terms
                        bool hasNetworkAdapterType = !string.IsNullOrEmpty(adapterType) &&
                               (adapterType.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                                adapterType.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                                adapterType.Contains("802.11", StringComparison.OrdinalIgnoreCase));
                        
                        // Include if Description contains network-related terms
                        bool hasNetworkDescription = !string.IsNullOrEmpty(description) &&
                               (description.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("802.11", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Network", StringComparison.OrdinalIgnoreCase));
                        
                        // Include if Name contains network-related terms
                        bool hasNetworkName = !string.IsNullOrEmpty(name) &&
                               (name.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("802.11", StringComparison.OrdinalIgnoreCase));
                        
                        // Exclude virtual adapters based on PNP Device ID or description
                        bool isVirtual = !string.IsNullOrEmpty(pnpDeviceId) &&
                               (pnpDeviceId.Contains("ROOT\\", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Tunnel", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("TAP", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("VPN", StringComparison.OrdinalIgnoreCase));
                        
                        // Include physical adapters and exclude obvious virtual ones
                        return (hasNetworkAdapterType || hasNetworkDescription || hasNetworkName) && 
                               !isVirtual &&
                               (physicalAdapter == null || (bool)physicalAdapter != false);
                    });

                foreach (var networkAdapter in physicalAdapters)
                {
                    var adapter = new EnhancedNetworkAdapter();
                    
                    // Get adapter name and basic info
                    adapter.AdapterName = networkAdapter["NetConnectionID"]?.ToString() ?? 
                                         networkAdapter["Description"]?.ToString() ?? "Unknown Adapter";
                    
                    // Get adapter type and connection status
                    adapter.AdapterType = networkAdapter["AdapterType"]?.ToString() ?? "Unknown";
                    adapter.NetConnectionStatus = networkAdapter["NetConnectionStatus"]?.ToString() ?? "Unknown";
                    
                    // Get MAC address
                    adapter.MACAddress = networkAdapter["MACAddress"]?.ToString() ?? "N/A";
                    
                    // Get link speed
                    var speed = networkAdapter["Speed"];
                    if (speed != null && long.TryParse(speed.ToString(), out var speedValue))
                    {
                        adapter.LinkSpeed = $"{Math.Round(speedValue / 1_000_000.0, 2)} MB/second";
                    }
                    else
                    {
                        adapter.LinkSpeed = "N/A";
                    }

                    // Get network configuration details
                    var index = networkAdapter["Index"]?.ToString();
                    if (!string.IsNullOrEmpty(index))
                    {
                        var config = GetLocalWmiObjects("Win32_NetworkAdapterConfiguration")
                            .FirstOrDefault(c => c["Index"]?.ToString() == index);
                        
                        if (config != null)
                        {
                            var ipAddresses = config["IPAddress"] as string[];
                            adapter.IPAddress = ipAddresses?.FirstOrDefault() ?? "N/A";
                            
                            var subnets = config["IPSubnet"] as string[];
                            adapter.IPSubnet = subnets?.FirstOrDefault() ?? "N/A";
                            
                            var gateways = config["DefaultIPGateway"] as string[];
                            adapter.DefaultGateway = gateways != null ? string.Join(" | ", gateways) : "N/A";
                            
                            var dnsServers = config["DNSServerSearchOrder"] as string[];
                            adapter.DNSServers = dnsServers != null ? string.Join(" | ", dnsServers) : "N/A";
                            
                            adapter.DHCPEnabled = config["DHCPEnabled"]?.ToString() ?? "N/A";
                            adapter.DHCPServer = config["DHCPServer"]?.ToString() ?? "N/A";
                            
                            // DHCP lease times
                            var dhcpObtained = config["DHCPLeaseObtained"]?.ToString();
                            if (!string.IsNullOrEmpty(dhcpObtained) && dhcpObtained.Length >= 14)
                            {
                                if (DateTime.TryParseExact(dhcpObtained.Substring(0, 14), "yyyyMMddHHmmss", null, DateTimeStyles.None, out var obtainedDate))
                                {
                                    adapter.DHCPStart = obtainedDate.ToString();
                                }
                            }
                            
                            var dhcpExpires = config["DHCPLeaseExpires"]?.ToString();
                            if (!string.IsNullOrEmpty(dhcpExpires) && dhcpExpires.Length >= 14)
                            {
                                if (DateTime.TryParseExact(dhcpExpires.Substring(0, 14), "yyyyMMddHHmmss", null, DateTimeStyles.None, out var expiresDate))
                                {
                                    adapter.DHCPExpires = expiresDate.ToString();
                                }
                            }
                        }
                        else
                        {
                            // No configuration available (adapter disabled or no IP)
                            adapter.IPAddress = "Not configured";
                            adapter.IPSubnet = "N/A";
                            adapter.DefaultGateway = "N/A";
                            adapter.DNSServers = "N/A";
                            adapter.DHCPEnabled = "N/A";
                            adapter.DHCPServer = "N/A";
                        }
                    }

                    networkAdapters.Add(adapter);
                }

                computerInfo.NetworkAdapters = networkAdapters;
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"Network info error: {ex.Message}; ";
            }
        }

        private static void CollectRemoteNetworkInfo(EnhancedComputerInfo computerInfo, ManagementScope scope)
        {
            try
            {
                var networkAdapters = new List<EnhancedNetworkAdapter>();

                // Get all physical network adapters (including wireless) with more lenient filtering for remote computers
                var physicalAdapters = GetRemoteWmiObjects(scope, "Win32_NetworkAdapter")
                    .Where(adapter => 
                    {
                        var adapterType = adapter["AdapterType"]?.ToString() ?? "";
                        var description = adapter["Description"]?.ToString() ?? "";
                        var name = adapter["Name"]?.ToString() ?? "";
                        var netConnectionId = adapter["NetConnectionID"]?.ToString() ?? "";
                        var physicalAdapter = adapter["PhysicalAdapter"];
                        var pnpDeviceId = adapter["PNPDeviceID"]?.ToString() ?? "";
                        
                        // More comprehensive filtering for remote systems
                        // Include if AdapterType contains network-related terms
                        bool hasNetworkAdapterType = !string.IsNullOrEmpty(adapterType) &&
                               (adapterType.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                                adapterType.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                                adapterType.Contains("802.11", StringComparison.OrdinalIgnoreCase));
                        
                        // Include if Description contains network-related terms
                        bool hasNetworkDescription = !string.IsNullOrEmpty(description) &&
                               (description.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("802.11", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Network", StringComparison.OrdinalIgnoreCase));
                        
                        // Include if Name contains network-related terms
                        bool hasNetworkName = !string.IsNullOrEmpty(name) &&
                               (name.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("802.11", StringComparison.OrdinalIgnoreCase));
                        
                        // Exclude virtual adapters based on PNP Device ID or description
                        bool isVirtual = !string.IsNullOrEmpty(pnpDeviceId) &&
                               (pnpDeviceId.Contains("ROOT\\", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("Tunnel", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("TAP", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("VPN", StringComparison.OrdinalIgnoreCase));
                        
                        // Include physical adapters and exclude obvious virtual ones
                        return (hasNetworkAdapterType || hasNetworkDescription || hasNetworkName) && 
                               !isVirtual &&
                               (physicalAdapter == null || (bool)physicalAdapter != false);
                    });

                foreach (var networkAdapter in physicalAdapters)
                {
                    var adapter = new EnhancedNetworkAdapter();
                    
                    // Get adapter name and basic info
                    adapter.AdapterName = networkAdapter["NetConnectionID"]?.ToString() ?? 
                                         networkAdapter["Description"]?.ToString() ?? "Unknown Adapter";
                    
                    // Get adapter type and connection status
                    adapter.AdapterType = networkAdapter["AdapterType"]?.ToString() ?? "Unknown";
                    adapter.NetConnectionStatus = networkAdapter["NetConnectionStatus"]?.ToString() ?? "Unknown";
                    
                    // Get MAC address
                    adapter.MACAddress = networkAdapter["MACAddress"]?.ToString() ?? "N/A";
                    
                    // Get link speed
                    var speed = networkAdapter["Speed"];
                    if (speed != null && long.TryParse(speed.ToString(), out var speedValue))
                    {
                        adapter.LinkSpeed = $"{Math.Round(speedValue / 1_000_000.0, 2)} MB/second";
                    }
                    else
                    {
                        adapter.LinkSpeed = "N/A";
                    }

                    // Get network configuration details
                    var index = networkAdapter["Index"]?.ToString();
                    if (!string.IsNullOrEmpty(index))
                    {
                        var config = GetRemoteWmiObjects(scope, "Win32_NetworkAdapterConfiguration")
                            .FirstOrDefault(c => c["Index"]?.ToString() == index);
                        
                        if (config != null)
                        {
                            var ipAddresses = config["IPAddress"] as string[];
                            adapter.IPAddress = ipAddresses?.FirstOrDefault() ?? "N/A";
                            
                            var subnets = config["IPSubnet"] as string[];
                            adapter.IPSubnet = subnets?.FirstOrDefault() ?? "N/A";
                            
                            var gateways = config["DefaultIPGateway"] as string[];
                            adapter.DefaultGateway = gateways != null ? string.Join(" | ", gateways) : "N/A";
                            
                            var dnsServers = config["DNSServerSearchOrder"] as string[];
                            adapter.DNSServers = dnsServers != null ? string.Join(" | ", dnsServers) : "N/A";
                            
                            adapter.DHCPEnabled = config["DHCPEnabled"]?.ToString() ?? "N/A";
                            adapter.DHCPServer = config["DHCPServer"]?.ToString() ?? "N/A";
                            
                            // DHCP lease times
                            var dhcpObtained = config["DHCPLeaseObtained"]?.ToString();
                            if (!string.IsNullOrEmpty(dhcpObtained) && dhcpObtained.Length >= 14)
                            {
                                if (DateTime.TryParseExact(dhcpObtained.Substring(0, 14), "yyyyMMddHHmmss", null, DateTimeStyles.None, out var obtainedDate))
                                {
                                    adapter.DHCPStart = obtainedDate.ToString();
                                }
                            }
                            
                            var dhcpExpires = config["DHCPLeaseExpires"]?.ToString();
                            if (!string.IsNullOrEmpty(dhcpExpires) && dhcpExpires.Length >= 14)
                            {
                                if (DateTime.TryParseExact(dhcpExpires.Substring(0, 14), "yyyyMMddHHmmss", null, DateTimeStyles.None, out var expiresDate))
                                {
                                    adapter.DHCPExpires = expiresDate.ToString();
                                }
                            }
                        }
                        else
                        {
                            // No configuration available (adapter disabled or no IP)
                            adapter.IPAddress = "Not configured";
                            adapter.IPSubnet = "N/A";
                            adapter.DefaultGateway = "N/A";
                            adapter.DNSServers = "N/A";
                            adapter.DHCPEnabled = "N/A";
                            adapter.DHCPServer = "N/A";
                        }
                    }

                    networkAdapters.Add(adapter);
                }

                computerInfo.NetworkAdapters = networkAdapters;
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"Remote network info error: {ex.Message}; ";
            }
        }

        #endregion

        #region Physical Disk Information Collection

        private static void CollectPhysicalDiskInfo(EnhancedComputerInfo computerInfo)
        {
            try
            {
                var physicalDisks = new List<EnhancedPhysicalDisk>();

                // Get physical disks
                var diskDrives = GetLocalWmiObjects("Win32_DiskDrive");
                var msftDisks = GetLocalWmiObjects("MSFT_PhysicalDisk", "root\\Microsoft\\Windows\\Storage");

                foreach (var diskDrive in diskDrives)
                {
                    var disk = new EnhancedPhysicalDisk();
                    
                    disk.DiskName = diskDrive["Name"]?.ToString() ?? "N/A";
                    
                    var index = diskDrive["Index"]?.ToString();
                    var msftDisk = msftDisks.FirstOrDefault(d => d["DeviceID"]?.ToString() == index);
                    
                    if (msftDisk != null)
                    {
                        disk.DiskModel = msftDisk["Model"]?.ToString() ?? "N/A";
                        disk.DiskFirmware = msftDisk["FirmwareVersion"]?.ToString() ?? "N/A";
                        
                        var busType = msftDisk["BusType"];
                        if (busType != null && int.TryParse(busType.ToString(), out var busTypeValue))
                        {
                            disk.DiskType = GetBusTypeName(busTypeValue);
                        }
                    }
                    
                    var size = diskDrive["Size"];
                    if (size != null && long.TryParse(size.ToString(), out var sizeValue))
                    {
                        disk.DiskCapacity = $"{Math.Round(sizeValue / 1_000_000_000.0, 2)}GB";
                    }

                    // Get partitions and logical disks
                    var diskPath = diskDrive["__PATH"]?.ToString();
                    if (!string.IsNullOrEmpty(diskPath))
                    {
                        CollectDiskPartitions(disk, diskPath);
                    }

                    physicalDisks.Add(disk);
                }

                computerInfo.PhysicalDisks = physicalDisks;
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"Disk info error: {ex.Message}; ";
            }
        }

        private static void CollectRemotePhysicalDiskInfo(EnhancedComputerInfo computerInfo, ManagementScope scope, string computerName)
        {
            try
            {
                var physicalDisks = new List<EnhancedPhysicalDisk>();

                // Get physical disks
                var diskDrives = GetRemoteWmiObjects(scope, "Win32_DiskDrive");
                
                // Try to get MSFT_PhysicalDisk info (may not be available on older systems)
                List<ManagementBaseObject> msftDisks = new List<ManagementBaseObject>();
                try
                {
                    var storageScope = new ManagementScope($"\\\\{computerName}\\root\\Microsoft\\Windows\\Storage");
                    storageScope.Connect();
                    msftDisks = GetRemoteWmiObjects(storageScope, "MSFT_PhysicalDisk");
                }
                catch
                {
                    // MSFT_PhysicalDisk not available, continue without it
                }

                foreach (var diskDrive in diskDrives)
                {
                    var disk = new EnhancedPhysicalDisk();
                    
                    disk.DiskName = diskDrive["Name"]?.ToString() ?? "N/A";
                    
                    var index = diskDrive["Index"]?.ToString();
                    var msftDisk = msftDisks.FirstOrDefault(d => d["DeviceID"]?.ToString() == index);
                    
                    if (msftDisk != null)
                    {
                        disk.DiskModel = msftDisk["Model"]?.ToString() ?? "N/A";
                        disk.DiskFirmware = msftDisk["FirmwareVersion"]?.ToString() ?? "N/A";
                        
                        var busType = msftDisk["BusType"];
                        if (busType != null && int.TryParse(busType.ToString(), out var busTypeValue))
                        {
                            disk.DiskType = GetBusTypeName(busTypeValue);
                        }
                    }
                    
                    var size = diskDrive["Size"];
                    if (size != null && long.TryParse(size.ToString(), out var sizeValue))
                    {
                        disk.DiskCapacity = $"{Math.Round(sizeValue / 1_000_000_000.0, 2)}GB";
                    }

                    // Get partitions and logical disks
                    var diskPath = diskDrive["__PATH"]?.ToString();
                    if (!string.IsNullOrEmpty(diskPath))
                    {
                        CollectRemoteDiskPartitions(disk, diskPath, scope);
                    }

                    physicalDisks.Add(disk);
                }

                computerInfo.PhysicalDisks = physicalDisks;
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"Remote disk info error: {ex.Message}; ";
            }
        }

        #endregion

        #region Physical Memory Information Collection

        private static void CollectPhysicalMemoryInfo(EnhancedComputerInfo computerInfo)
        {
            try
            {
                var memoryModules = new List<EnhancedMemoryModule>();

                var ramModules = GetLocalWmiObjects("Win32_PhysicalMemory");

                foreach (var ram in ramModules)
                {
                    var module = new EnhancedMemoryModule();
                    
                    module.Slot = ram["DeviceLocator"]?.ToString() ?? "N/A";
                    module.Manufacturer = ram["Manufacturer"]?.ToString() ?? "N/A";
                    module.Model = ram["PartNumber"]?.ToString() ?? "N/A";
                    module.Serial = ram["SerialNumber"]?.ToString() ?? "N/A";
                    
                    var capacity = ram["Capacity"];
                    if (capacity != null && long.TryParse(capacity.ToString(), out var capacityValue))
                    {
                        module.Capacity = $"{capacityValue / 1_000_000_000}GB";
                    }
                    
                    module.Speed = ram["Speed"]?.ToString() ?? "N/A";
                    
                    var formFactor = ram["FormFactor"];
                    if (formFactor != null && int.TryParse(formFactor.ToString(), out var formFactorValue))
                    {
                        module.FormFactor = GetFormFactorName(formFactorValue);
                    }
                    
                    var memoryType = ram["SMBIOSMemoryType"];
                    if (memoryType != null && int.TryParse(memoryType.ToString(), out var memoryTypeValue))
                    {
                        module.RAMType = GetMemoryTypeName(memoryTypeValue);
                    }

                    memoryModules.Add(module);
                }

                computerInfo.PhysicalMemory = memoryModules;
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"Memory info error: {ex.Message}; ";
            }
        }

        private static void CollectRemotePhysicalMemoryInfo(EnhancedComputerInfo computerInfo, ManagementScope scope)
        {
            try
            {
                var memoryModules = new List<EnhancedMemoryModule>();

                var ramModules = GetRemoteWmiObjects(scope, "Win32_PhysicalMemory");

                foreach (var ram in ramModules)
                {
                    var module = new EnhancedMemoryModule();
                    
                    module.Slot = ram["DeviceLocator"]?.ToString() ?? "N/A";
                    module.Manufacturer = ram["Manufacturer"]?.ToString() ?? "N/A";
                    module.Model = ram["PartNumber"]?.ToString() ?? "N/A";
                    module.Serial = ram["SerialNumber"]?.ToString() ?? "N/A";
                    
                    var capacity = ram["Capacity"];
                    if (capacity != null && long.TryParse(capacity.ToString(), out var capacityValue))
                    {
                        module.Capacity = $"{capacityValue / 1_000_000_000}GB";
                    }
                    
                    module.Speed = ram["Speed"]?.ToString() ?? "N/A";
                    
                    var formFactor = ram["FormFactor"];
                    if (formFactor != null && int.TryParse(formFactor.ToString(), out var formFactorValue))
                    {
                        module.FormFactor = GetFormFactorName(formFactorValue);
                    }
                    
                    var memoryType = ram["SMBIOSMemoryType"];
                    if (memoryType != null && int.TryParse(memoryType.ToString(), out var memoryTypeValue))
                    {
                        module.RAMType = GetMemoryTypeName(memoryTypeValue);
                    }

                    memoryModules.Add(module);
                }

                computerInfo.PhysicalMemory = memoryModules;
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"Remote memory info error: {ex.Message}; ";
            }
        }

        #endregion

        #region Active Directory Information Collection

        private static void CollectActiveDirectoryInfo(EnhancedComputerInfo computerInfo)
        {
            try
            {
                string searchFilter = $"(DNSHostName={computerInfo.ComputerName}*)";
                var directoryEntry = new DirectoryEntry("LDAP://rootDSE");
                var defaultNamingContextValue = directoryEntry.Properties["defaultNamingContext"]?.Value;
                if (defaultNamingContextValue == null) return;
                
                var defaultNamingContext = defaultNamingContextValue.ToString() ?? "";
                if (string.IsNullOrEmpty(defaultNamingContext)) return;
                
                using var domainEntry = new DirectoryEntry($"LDAP://{defaultNamingContext}");
                using var searcher = new DirectorySearcher(domainEntry)
                {
                    Filter = searchFilter,
                    SearchScope = SearchScope.Subtree
                };
                
                searcher.PropertiesToLoad.AddRange(new[] { "name", "distinguishedName", "userAccountControl", "dNSHostName", "lastLogonDate" });

                var result = searcher.FindOne();
                if (result != null)
                {
                    var adInfo = new EnhancedActiveDirectoryInfo();
                    
                    adInfo.Name = result.Properties["name"].Count > 0 ? (result.Properties["name"][0]?.ToString() ?? "N/A") : "N/A";
                    adInfo.DistinguishedName = result.Properties["distinguishedName"].Count > 0 ? (result.Properties["distinguishedName"][0]?.ToString() ?? "N/A") : "N/A";
                    adInfo.DNSHostName = result.Properties["dNSHostName"].Count > 0 ? (result.Properties["dNSHostName"][0]?.ToString() ?? "N/A") : "N/A";
                    
                    if (result.Properties["userAccountControl"].Count > 0 && int.TryParse(result.Properties["userAccountControl"][0]?.ToString(), out var uacValue))
                    {
                        adInfo.AccountEnabled = (uacValue & 0x2) == 0; // ADS_UF_ACCOUNTDISABLE
                    }
                    
                    if (result.Properties["lastLogonDate"].Count > 0 && DateTime.TryParse(result.Properties["lastLogonDate"][0]?.ToString(), out var lastLogon))
                    {
                        adInfo.LastContactDate = lastLogon.ToShortDateString();
                    }
                    
                    computerInfo.ActiveDirectoryInfo = adInfo;
                }
            }
            catch (Exception ex)
            {
                computerInfo.ErrorMessage += $"AD info error: {ex.Message}; ";
            }
        }

        #endregion

        #region Helper Methods

        private static List<ManagementBaseObject> GetLocalWmiObjects(string className, string namespacePath = "root\\cimv2")
        {
            var objects = new List<ManagementBaseObject>();
            try
            {
                var query = new ObjectQuery($"SELECT * FROM {className}");
                using var searcher = new ManagementObjectSearcher(namespacePath, query.QueryString);
                using var results = searcher.Get();
                
                foreach (ManagementObject obj in results)
                {
                    objects.Add(obj);
                }
            }
            catch
            {
                // Return empty list on error
            }
            return objects;
        }

        private static List<ManagementBaseObject> GetRemoteWmiObjects(ManagementScope scope, string className)
        {
            var objects = new List<ManagementBaseObject>();
            try
            {
                var query = new ObjectQuery($"SELECT * FROM {className}");
                using var searcher = new ManagementObjectSearcher(scope, query);
                using var results = searcher.Get();
                
                foreach (ManagementObject obj in results)
                {
                    objects.Add(obj);
                }
            }
            catch
            {
                // Return empty list on error
            }
            return objects;
        }

        private static string GetAssetTag()
        {
            try
            {
                // Try registry first (VWG specific)
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VWG\Inventory");
                var regValue = key?.GetValueNames().FirstOrDefault(name => name.Contains("AssetTag", StringComparison.OrdinalIgnoreCase));
                if (regValue != null && key != null)
                {
                    var assetTag = key.GetValue(regValue)?.ToString();
                    if (!string.IsNullOrEmpty(assetTag))
                        return assetTag;
                }
            }
            catch
            {
                return "N/A";
            }
            return "N/A";
        }

        /// <summary>
        /// Gets a registry value from a remote computer using WMI StdRegProv
        /// This is the C# equivalent of: Invoke-Command -ComputerName "RemotePC" -ScriptBlock { Get-ItemProperty -Path "HKLM:\SOFTWARE\..." }
        /// </summary>
        private static string GetRemoteRegistryValue(ManagementScope scope, string keyPath, string valueName)
        {
            try
            {
                // Create the registry provider scope
                var registryScope = new ManagementScope(scope.Path.ToString().Replace("root\\cimv2", "root\\default"));
                registryScope.Connect();

                // Use StdRegProv to access remote registry
                using var registryClass = new ManagementClass(registryScope, new ManagementPath("StdRegProv"), null);
                
                // Prepare method parameters
                var inParams = registryClass.GetMethodParameters("GetStringValue");
                inParams["hDefKey"] = 0x80000002; // HKEY_LOCAL_MACHINE
                inParams["sSubKeyName"] = "SOFTWARE\\VWG\\Inventory";
                inParams["sValueName"] = "AssetTag";

                // Execute the method
                var outParams = registryClass.InvokeMethod("GetStringValue", inParams, null);
                
                // Check return value (0 = success)
                var returnValue = (uint)outParams["ReturnValue"];
                if (returnValue == 0)
                {
                    var value = outParams["sValue"]?.ToString();
                    return !string.IsNullOrWhiteSpace(value) ? value : "N/A";
                }
                
                return "N/A";
            }
            catch
            {
                // Return N/A on any registry access error
                return "N/A";
            }
        }

        private static void CollectDiskPartitions(EnhancedPhysicalDisk disk, string diskPath)
        {
            try
            {
                if (string.IsNullOrEmpty(diskPath)) return;

                var partitionRelations = GetLocalWmiObjects("Win32_DiskDriveToDiskPartition")
                    .Where(rel => rel["Antecedent"]?.ToString() == diskPath);

                foreach (var relation in partitionRelations)
                {
                    var partitionPath = relation["Dependent"]?.ToString();
                    if (string.IsNullOrEmpty(partitionPath)) continue;

                    // Extract partition DeviceID from path
                    var match = Regex.Match(partitionPath, @"DeviceID=""([^""]+)""");
                    if (!match.Success) continue;

                    var partitionDeviceId = match.Groups[1].Value;
                    var partition = GetLocalWmiObjects("Win32_DiskPartition")
                        .FirstOrDefault(p => p["DeviceID"]?.ToString() == partitionDeviceId);

                    if (partition != null)
                    {
                        var partInfo = new EnhancedDiskPartition
                        {
                            Name = partition["Name"]?.ToString() ?? "N/A",
                            BootPartition = partition["BootPartition"]?.ToString() ?? "N/A",
                            Type = partition["Type"]?.ToString() ?? "N/A"
                        };

                        var size = partition["Size"];
                        if (size != null && long.TryParse(size.ToString(), out var sizeValue))
                        {
                            partInfo.Size = $"{Math.Round(sizeValue / 1_000_000_000.0, 2)}GB";
                        }

                        disk.Partitions.Add(partInfo);

                        // Get logical disk for this partition
                        var logicalRelations = GetLocalWmiObjects("Win32_LogicalDiskToPartition")
                            .Where(rel => rel["Antecedent"]?.ToString()?.Contains(partitionDeviceId) == true);

                        foreach (var logicalRelation in logicalRelations)
                        {
                            var logicalPath = logicalRelation["Dependent"]?.ToString();
                            if (string.IsNullOrEmpty(logicalPath)) continue;
                            
                            var logicalMatch = Regex.Match(logicalPath, @"DeviceID=""([^""]+)""");
                            if (!logicalMatch.Success) continue;

                            var logicalDeviceId = logicalMatch.Groups[1].Value;
                            var logicalDisk = GetLocalWmiObjects("Win32_LogicalDisk")
                                .FirstOrDefault(ld => ld["DeviceID"]?.ToString() == logicalDeviceId);

                            if (logicalDisk != null)
                            {
                                var volume = new EnhancedLogicalVolume
                                {
                                    VolumeID = logicalDisk["DeviceID"]?.ToString() ?? "N/A",
                                    FileSystem = logicalDisk["FileSystem"]?.ToString() ?? "N/A"
                                };

                                var driveType = logicalDisk["DriveType"];
                                if (driveType != null && int.TryParse(driveType.ToString(), out var driveTypeValue))
                                {
                                    volume.DriveType = GetDriveTypeName(driveTypeValue);
                                }

                                var mediaType = logicalDisk["MediaType"];
                                if (mediaType != null && int.TryParse(mediaType.ToString(), out var mediaTypeValue))
                                {
                                    volume.MediaType = GetMediaTypeName(mediaTypeValue);
                                }

                                var capacity = logicalDisk["Size"];
                                var freeSpace = logicalDisk["FreeSpace"];
                                if (capacity != null && long.TryParse(capacity.ToString(), out var capacityValue) &&
                                    freeSpace != null && long.TryParse(freeSpace.ToString(), out var freeSpaceValue))
                                {
                                    var capacityGB = Math.Round(capacityValue / 1_000_000_000.0, 2);
                                    var freeSpaceGB = Math.Round(freeSpaceValue / 1_000_000_000.0, 2);
                                    var freeSpacePercent = Math.Round((double)freeSpaceValue / capacityValue * 100, 1);
                                    
                                    volume.Capacity = $"{capacityGB}GB";
                                    volume.FreeSpace = $"{freeSpaceGB}GB ({freeSpacePercent}% FreeSpace)";
                                }

                                disk.LogicalVolumes.Add(volume);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Continue without partition info on error
            }
        }

        private static void CollectRemoteDiskPartitions(EnhancedPhysicalDisk disk, string diskPath, ManagementScope scope)
        {
            try
            {
                if (string.IsNullOrEmpty(diskPath)) return;

                var partitionRelations = GetRemoteWmiObjects(scope, "Win32_DiskDriveToDiskPartition")
                    .Where(rel => rel["Antecedent"]?.ToString() == diskPath);

                foreach (var relation in partitionRelations)
                {
                    var partitionPath = relation["Dependent"]?.ToString();
                    if (string.IsNullOrEmpty(partitionPath)) continue;

                    // Extract partition DeviceID from path
                    var match = Regex.Match(partitionPath, @"DeviceID=""([^""]+)""");
                    if (!match.Success) continue;

                    var partitionDeviceId = match.Groups[1].Value;
                    var partition = GetRemoteWmiObjects(scope, "Win32_DiskPartition")
                        .FirstOrDefault(p => p["DeviceID"]?.ToString() == partitionDeviceId);

                    if (partition != null)
                    {
                        var partInfo = new EnhancedDiskPartition
                        {
                            Name = partition["Name"]?.ToString() ?? "N/A",
                            BootPartition = partition["BootPartition"]?.ToString() ?? "N/A",
                            Type = partition["Type"]?.ToString() ?? "N/A"
                        };

                        var size = partition["Size"];
                        if (size != null && long.TryParse(size.ToString(), out var sizeValue))
                        {
                            partInfo.Size = $"{Math.Round(sizeValue / 1_000_000_000.0, 2)}GB";
                        }

                        disk.Partitions.Add(partInfo);

                        // Get logical disk for this partition
                        var logicalRelations = GetRemoteWmiObjects(scope, "Win32_LogicalDiskToPartition")
                            .Where(rel => rel["Antecedent"]?.ToString()?.Contains(partitionDeviceId) == true);

                        foreach (var logicalRelation in logicalRelations)
                        {
                            var logicalPath = logicalRelation["Dependent"]?.ToString();
                            if (string.IsNullOrEmpty(logicalPath)) continue;
                            
                            var logicalMatch = Regex.Match(logicalPath, @"DeviceID=""([^""]+)""");
                            if (!logicalMatch.Success) continue;

                            var logicalDeviceId = logicalMatch.Groups[1].Value;
                            var logicalDisk = GetRemoteWmiObjects(scope, "Win32_LogicalDisk")
                                .FirstOrDefault(ld => ld["DeviceID"]?.ToString() == logicalDeviceId);

                            if (logicalDisk != null)
                            {
                                var volume = new EnhancedLogicalVolume
                                {
                                    VolumeID = logicalDisk["DeviceID"]?.ToString() ?? "N/A",
                                    FileSystem = logicalDisk["FileSystem"]?.ToString() ?? "N/A"
                                };

                                var driveType = logicalDisk["DriveType"];
                                if (driveType != null && int.TryParse(driveType.ToString(), out var driveTypeValue))
                                {
                                    volume.DriveType = GetDriveTypeName(driveTypeValue);
                                }

                                var mediaType = logicalDisk["MediaType"];
                                if (mediaType != null && int.TryParse(mediaType.ToString(), out var mediaTypeValue))
                                {
                                    volume.MediaType = GetMediaTypeName(mediaTypeValue);
                                }

                                var capacity = logicalDisk["Size"];
                                var freeSpace = logicalDisk["FreeSpace"];
                                if (capacity != null && long.TryParse(capacity.ToString(), out var capacityValue) &&
                                    freeSpace != null && long.TryParse(freeSpace.ToString(), out var freeSpaceValue))
                                {
                                    var capacityGB = Math.Round(capacityValue / 1_000_000_000.0, 2);
                                    var freeSpaceGB = Math.Round(freeSpaceValue / 1_000_000_000.0, 2);
                                    var freeSpacePercent = Math.Round((double)freeSpaceValue / capacityValue * 100, 1);
                                    
                                    volume.Capacity = $"{capacityGB}GB";
                                    volume.FreeSpace = $"{freeSpaceGB}GB ({freeSpacePercent}% FreeSpace)";
                                }

                                disk.LogicalVolumes.Add(volume);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Continue without partition info on error
            }
        }

        private static string GetBusTypeName(int busType)
        {
            return busType switch
            {
                0 => "Unknown",
                1 => "SCSI",
                2 => "ATAPI", 
                3 => "ATA",
                4 => "IEEE 1394",
                5 => "SSA",
                6 => "Fibre Channel",
                7 => "USB",
                8 => "RAID",
                9 => "iSCSI",
                10 => "Serial Attached SCSI (SAS)",
                11 => "Serial ATA (SATA)",
                12 => "Secure Digital Card (SDCard)",
                13 => "Multimedia Card (MMC)",
                14 => "MAX - This value is reserved for system use.",
                15 => "File-Backed Virtual",
                16 => "Storage Spaces",
                17 => "NVMe",
                18 => "Microsoft Reserved - This value is reserved for system use.",
                _ => "Unknown"
            };
        }

        private static string GetFormFactorName(int formFactor)
        {
            return formFactor switch
            {
                1 => "Other",
                2 => "SIP",
                3 => "DIP",
                4 => "ZIP",
                5 => "SOJ",
                6 => "Proprietary",
                7 => "SIMM",
                8 => "DIMM",
                9 => "TSOP",
                10 => "PGA",
                11 => "RIMM",
                12 => "SODIMM",
                13 => "SRIMM",
                14 => "SMD",
                15 => "SSMP",
                16 => "QFP",
                17 => "TQFP",
                18 => "SOIC",
                19 => "LCC",
                20 => "PLCC",
                21 => "BGA",
                22 => "FPBGA",
                23 => "LGA",
                24 => "FB-DIMM",
                _ => "Unknown"
            };
        }

        private static string GetMemoryTypeName(int memoryType)
        {
            return memoryType switch
            {
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
                _ => "Unknown"
            };
        }

        private static string GetDriveTypeName(int driveType)
        {
            return driveType switch
            {
                0 => "Unknown",
                1 => "No Root Directory",
                2 => "Removable Disk",
                3 => "Local Disk",
                4 => "Network Drive",
                5 => "Compact Disc",
                6 => "RAM Disk",
                _ => "Unknown"
            };
        }

        private static string GetMediaTypeName(int mediaType)
        {
            return mediaType switch
            {
                0 => "Format is unknown",
                1 => "5 1/4-Inch Floppy Disk - 1.2 MB - 512 bytes/sector",
                2 => "3 1/2-Inch Floppy Disk - 1.44 MB -512 bytes/sector",
                3 => "3 1/2-Inch Floppy Disk - 2.88 MB - 512 bytes/sector",
                4 => "3 1/2-Inch Floppy Disk - 20.8 MB - 512 bytes/sector",
                5 => "3 1/2-Inch Floppy Disk - 720 KB - 512 bytes/sector",
                6 => "5 1/4-Inch Floppy Disk - 360 KB - 512 bytes/sector",
                7 => "5 1/4-Inch Floppy Disk - 320 KB - 512 bytes/sector",
                8 => "5 1/4-Inch Floppy Disk - 320 KB - 1024 bytes/sector",
                9 => "5 1/4-Inch Floppy Disk - 180 KB - 512 bytes/sector",
                10 => "5 1/4-Inch Floppy Disk - 160 KB - 512 bytes/sector",
                11 => "Removable media other than floppy",
                12 => "Fixed hard disk media",
                13 => "3 1/2-Inch Floppy Disk - 120 MB - 512 bytes/sector",
                14 => "3 1/2-Inch Floppy Disk - 640 KB - 512 bytes/sector",
                15 => "5 1/4-Inch Floppy Disk - 640 KB - 512 bytes/sector",
                16 => "5 1/4-Inch Floppy Disk - 720 KB - 512 bytes/sector",
                17 => "3 1/2-Inch Floppy Disk - 1.2 MB - 512 bytes/sector",
                18 => "3 1/2-Inch Floppy Disk - 1.23 MB - 1024 bytes/sector",
                19 => "5 1/4-Inch Floppy Disk - 1.23 MB - 1024 bytes/sector",
                20 => "3 1/2-Inch Floppy Disk - 128 MB - 512 bytes/sector",
                21 => "3 1/2-Inch Floppy Disk - 230 MB - 512 bytes/sector",
                22 => "8-Inch Floppy Disk - 256 KB - 128 bytes/sector",
                _ => "Unknown"
            };
        }

        #endregion
    }

    #region Enhanced Data Models

    /// <summary>
    /// Enhanced computer information matching PowerShell Get-ComputerInfo-v2.0.ps1 output
    /// </summary>
    public class EnhancedComputerInfo
    {
        // Hardware Information
        public string ComputerName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string AssetTag { get; set; } = string.Empty;
        public string LocalTime { get; set; } = string.Empty;
        public string Uptime { get; set; } = string.Empty;
        public string BIOSReleaseDate { get; set; } = string.Empty;
        public string BIOSAge { get; set; } = string.Empty;
        public string BIOSVersion { get; set; } = string.Empty;
        public string ProcessorName { get; set; } = string.Empty;

        // Operating System Information
        public string OperatingSystem { get; set; } = string.Empty;
        public string OSArchitecture { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public string OSInstallDate { get; set; } = string.Empty;

        // Network Information
        public List<EnhancedNetworkAdapter> NetworkAdapters { get; set; } = new();

        // Physical Disk Information
        public List<EnhancedPhysicalDisk> PhysicalDisks { get; set; } = new();

        // Physical Memory Information
        public List<EnhancedMemoryModule> PhysicalMemory { get; set; } = new();

        // Active Directory Information
        public EnhancedActiveDirectoryInfo? ActiveDirectoryInfo { get; set; }

        // Error Information
        public string ErrorMessage { get; set; } = string.Empty;

        // Computed Properties
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public string HardwareInfo => $"{Manufacturer} {Model}".Trim();
        public bool HasNetworkAdapters => NetworkAdapters.Any();
        public bool HasPhysicalDisks => PhysicalDisks.Any();
        public bool HasPhysicalMemory => PhysicalMemory.Any();
        public bool HasActiveDirectoryInfo => ActiveDirectoryInfo != null;
        public bool HasAssetTag => !string.IsNullOrEmpty(AssetTag);
        public bool HasSerialNumber => !string.IsNullOrEmpty(SerialNumber);
        public bool HasAssetInfo => HasAssetTag || HasSerialNumber;
        public string DisplayName => ComputerName;
        public bool IsError => HasError;
        
        // Memory computed properties
        public bool HasMemoryInfo => PhysicalMemory.Any();
        public string RAMType => PhysicalMemory.FirstOrDefault()?.RAMType ?? "N/A";
        public string RAMInstalled => PhysicalMemory.Any() ? 
            $"{PhysicalMemory.Sum(m => decimal.TryParse(m.Capacity?.Replace("GB", ""), out var cap) ? cap : 0):F1} GB" : "N/A";
        public string RAMUsageInfo => "Usage data not available"; // TODO: Add memory usage collection
        
        // Drive computed properties  
        public bool HasDriveInfo => PhysicalDisks.Any(d => d.LogicalVolumes.Any());
        public List<DriveDetailInfo> DriveDetails => PhysicalDisks
            .SelectMany(disk => disk.LogicalVolumes.Select(volume => new DriveDetailInfo
            {
                DriveLetter = volume.VolumeID,
                SizeInfo = volume.Capacity,
                FreeSpaceInfo = volume.FreeSpace,
                FileSystem = volume.FileSystem
            }))
            .ToList();
        
        // Network computed properties
        public string DomainOrWorkgroupDisplay => ActiveDirectoryInfo?.DNSHostName ?? "N/A";
        public string IsDomainJoinedDisplay => (ActiveDirectoryInfo != null).ToString();
        public string DNSServersDisplay => NetworkAdapters.FirstOrDefault()?.DNSServers ?? "N/A";
        public string DefaultGatewaysDisplay => NetworkAdapters.FirstOrDefault()?.DefaultGateway ?? "N/A";
        public string SubnetMasksDisplay => NetworkAdapters.FirstOrDefault()?.IPSubnet ?? "N/A";
    }

    public class EnhancedNetworkAdapter
    {
        public string AdapterName { get; set; } = string.Empty;
        public string MACAddress { get; set; } = string.Empty;
        public string LinkSpeed { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string IPSubnet { get; set; } = string.Empty;
        public string DefaultGateway { get; set; } = string.Empty;
        public string DNSServers { get; set; } = string.Empty;
        public string DHCPEnabled { get; set; } = string.Empty;
        public string DHCPServer { get; set; } = string.Empty;
        public string DHCPStart { get; set; } = string.Empty;
        public string DHCPExpires { get; set; } = string.Empty;
        public string AdapterType { get; set; } = string.Empty;
        public string NetConnectionStatus { get; set; } = string.Empty;
        
        // Computed properties for UI binding
        public string FormattedMACAddress 
        { 
            get
            {
                if (string.IsNullOrEmpty(MACAddress))
                    return string.Empty;
                
                // Remove any existing separators and ensure it's 12 characters
                var cleanMac = MACAddress.Replace(":", "").Replace("-", "").Replace(" ", "");
                
                // If not exactly 12 characters, return as-is
                if (cleanMac.Length != 12)
                    return MACAddress;
                    
                // Convert from format like "001122334455" to "00:11:22:33:44:55"
                return string.Join(":", Enumerable.Range(0, 6)
                    .Select(i => cleanMac.Substring(i * 2, 2)));
            }
        }
        
        public string SubnetMask => IPSubnet;
        public bool HasMACAddress => !string.IsNullOrEmpty(MACAddress);
        public bool HasIPAddress => !string.IsNullOrEmpty(IPAddress) && IPAddress != "N/A" && IPAddress != "Not configured";
        public bool HasSpSubnetMasksDisplayed => !string.IsNullOrEmpty(IPSubnet) && IPSubnet != "N/A";
        public bool IsWireless => AdapterType.Contains("Wireless", StringComparison.OrdinalIgnoreCase) || 
                                  AdapterType.Contains("802.11", StringComparison.OrdinalIgnoreCase) ||
                                  AdapterName.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                                  AdapterName.Contains("Wireless", StringComparison.OrdinalIgnoreCase);
        public bool IsConnected => NetConnectionStatus == "Connected" || NetConnectionStatus == "2";
        public string ConnectionStatus => IsConnected ? "Connected" : "Disconnected";
        
        // Display name with connection status for wireless adapters
        public string DisplayName => IsWireless ? $"{AdapterName} ({ConnectionStatus})" : AdapterName;
    }

    public class EnhancedPhysicalDisk
    {
        public string DiskName { get; set; } = string.Empty;
        public string DiskModel { get; set; } = string.Empty;
        public string DiskFirmware { get; set; } = string.Empty;
        public string DiskType { get; set; } = string.Empty;
        public string DiskCapacity { get; set; } = string.Empty;
        public List<EnhancedDiskPartition> Partitions { get; set; } = new();
        public List<EnhancedLogicalVolume> LogicalVolumes { get; set; } = new();
    }

    public class EnhancedDiskPartition
    {
        public string Name { get; set; } = string.Empty;
        public string BootPartition { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
    }

    public class EnhancedLogicalVolume
    {
        public string VolumeID { get; set; } = string.Empty;
        public string DriveType { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string FileSystem { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string FreeSpace { get; set; } = string.Empty;
    }

    public class EnhancedMemoryModule
    {
        public string Slot { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Serial { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string Speed { get; set; } = string.Empty;
        public string FormFactor { get; set; } = string.Empty;
        public string RAMType { get; set; } = string.Empty;
    }

    public class EnhancedActiveDirectoryInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DistinguishedName { get; set; } = string.Empty;
        public bool AccountEnabled { get; set; }
        public string DNSHostName { get; set; } = string.Empty;
        public string DNSIPAddress { get; set; } = string.Empty;
        public string LastContactDate { get; set; } = string.Empty;
    }

    public class DriveDetailInfo
    {
        public string DriveLetter { get; set; } = string.Empty;
        public string SizeInfo { get; set; } = string.Empty;
        public string FreeSpaceInfo { get; set; } = string.Empty;
        public string FileSystem { get; set; } = string.Empty;
    }

    #endregion
}
