using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp1.Models;
using MauiApp1.Interfaces;
using MauiApp1.Scripts;

namespace MauiApp1.ViewModels
{
    public class ComputerInfoViewModel : INotifyPropertyChanged
    {
        public ICommand UpdateSelectedCommand { get; }
        public ICommand UpdateAllCommand { get; }
        public ICommand AddToDatabaseCommand { get; }

        private string _targetInput = string.Empty;
        private string _results = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isScanning = false;
        private bool _hasResults = false;

        private readonly IDatabaseService _databaseService;
        private readonly IAddDeviceService _addDeviceService;
        private readonly IDialogService _dialogService;
        private ObservableCollection<ComputerInfoComparisonResult> _comparisonResults = new();
        private ObservableCollection<EnhancedComputerInfo> _enhancedComputerResults = new();

        public ComputerInfoViewModel(
            IDatabaseService databaseService, 
            IAddDeviceService addDeviceService,
            IDialogService dialogService)
        {
            _databaseService = databaseService;
            _addDeviceService = addDeviceService;
            _dialogService = dialogService;
            ScanCommand = new Command(async () => await ScanComputersAsync());
            ClearCommand = new Command(ClearResults);

            UpdateSelectedCommand = new Command<ComputerInfoComparisonResult>(async (comparison) => await UpdateSelectedFieldsAsync(comparison));
            UpdateAllCommand = new Command<ComputerInfoComparisonResult>(async (comparison) => await UpdateAllFieldsAsync(comparison));
            AddToDatabaseCommand = new Command<EnhancedComputerInfo>(async (computerInfo) => await AddComputerToDatabaseAsync(computerInfo));
        }

        // Update only checked fields
        private async Task UpdateSelectedFieldsAsync(ComputerInfoComparisonResult comparison)
        {
            if (comparison == null || comparison.DatabaseInfo == null) return;
            var dbDevice = comparison.DatabaseInfo;
            var enhanced = comparison.ScriptInfo;

            // Only update fields where the corresponding boolean is true
            if (comparison.UpdateSerialNumber) dbDevice.SerialNumber = enhanced.SerialNumber;
            if (comparison.UpdateAssetTag) dbDevice.AssetTag = enhanced.AssetTag;
            if (comparison.UpdateDomainName && enhanced.ActiveDirectoryInfo != null) 
                dbDevice.DomainName = enhanced.ActiveDirectoryInfo.DNSHostName;
            if (comparison.UpdateWorkgroup && enhanced.ActiveDirectoryInfo != null) 
                dbDevice.Workgroup = enhanced.ActiveDirectoryInfo.DNSHostName;
            if (comparison.UpdateIsDomainJoined) 
                dbDevice.IsDomainJoined = enhanced.ActiveDirectoryInfo != null;
            if (comparison.UpdateManufacturer) dbDevice.Manufacturer = enhanced.Manufacturer;
            if (comparison.UpdateModel) dbDevice.Model = enhanced.Model;
            if (comparison.UpdateCpuInfo) 
                dbDevice.CpuInfo = "CPU Info"; // TODO: Need to add CPU info to EnhancedComputerInfo
            if (comparison.UpdateTotalRamGb && enhanced.PhysicalMemory.Any())
            {
                var totalRamGB = enhanced.PhysicalMemory
                    .Where(m => !string.IsNullOrEmpty(m.Capacity) && m.Capacity.EndsWith("GB"))
                    .Sum(m => decimal.TryParse(m.Capacity.Replace("GB", ""), out var cap) ? cap : 0);
                dbDevice.TotalRamGb = totalRamGB;
            }
            if (comparison.UpdateRamType && enhanced.PhysicalMemory.Any()) 
                dbDevice.RamType = enhanced.PhysicalMemory.FirstOrDefault()?.RAMType;
            if (comparison.UpdateStorageInfo && enhanced.PhysicalDisks.Any()) 
                dbDevice.StorageInfo = string.Join("; ", enhanced.PhysicalDisks.Select(d => d.DiskCapacity));
            if (comparison.UpdateBiosVersion) dbDevice.BiosVersion = enhanced.BIOSVersion;
            if (comparison.UpdateOsName) dbDevice.OsName = enhanced.OperatingSystem;
            if (comparison.UpdateOsVersion) dbDevice.OSVersion = enhanced.OSVersion;
            if (comparison.UpdateOsArchitecture) dbDevice.OsArchitecture = enhanced.OSArchitecture;
            if (comparison.UpdatePrimaryIp && enhanced.NetworkAdapters.Any()) 
            {
                var primaryAdapter = FindPrimaryAdapter(enhanced, comparison.ScanTarget);
                dbDevice.PrimaryIp = primaryAdapter?.IPAddress;
            }
            if (comparison.UpdatePrimaryMac && enhanced.NetworkAdapters.Any()) 
            {
                var primaryAdapter = FindPrimaryAdapter(enhanced, comparison.ScanTarget);
                dbDevice.PrimaryMac = primaryAdapter?.MACAddress;
            }
            if (comparison.UpdateSecondaryIps && enhanced.NetworkAdapters.Count > 1) 
            {
                var primaryAdapter = FindPrimaryAdapter(enhanced, comparison.ScanTarget);
                var secondaryAdapters = enhanced.NetworkAdapters.Where(a => a != primaryAdapter && !string.IsNullOrEmpty(a.IPAddress)).ToList();
                
                // Assign to individual NIC properties
                if (secondaryAdapters.Count > 0) dbDevice.Nic2Ip = secondaryAdapters[0].IPAddress;
                if (secondaryAdapters.Count > 1) dbDevice.Nic3Ip = secondaryAdapters[1].IPAddress;
                if (secondaryAdapters.Count > 2) dbDevice.Nic4Ip = secondaryAdapters[2].IPAddress;
            }
            if (comparison.UpdateSecondaryMacs && enhanced.NetworkAdapters.Count > 1) 
            {
                var primaryAdapter = FindPrimaryAdapter(enhanced, comparison.ScanTarget);
                var secondaryAdapters = enhanced.NetworkAdapters.Where(a => a != primaryAdapter && !string.IsNullOrEmpty(a.MACAddress)).ToList();
                
                // Assign to individual NIC MAC properties
                if (secondaryAdapters.Count > 0) dbDevice.Nic2Mac = secondaryAdapters[0].MACAddress;
                if (secondaryAdapters.Count > 1) dbDevice.Nic3Mac = secondaryAdapters[1].MACAddress;
                if (secondaryAdapters.Count > 2) dbDevice.Nic4Mac = secondaryAdapters[2].MACAddress;
            }
            if (comparison.UpdateDnsServers && enhanced.NetworkAdapters.Any()) 
            {
                var primaryAdapter = FindPrimaryAdapter(enhanced, comparison.ScanTarget);
                if (!string.IsNullOrEmpty(primaryAdapter?.DNSServers))
                {
                    var dnsServers = primaryAdapter.DNSServers.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (dnsServers.Length > 0) dbDevice.PrimaryDns = dnsServers[0].Trim();
                    if (dnsServers.Length > 1) dbDevice.SecondaryDns = dnsServers[1].Trim();
                }
            }
            if (comparison.UpdateSubnetMasks && enhanced.NetworkAdapters.Any()) 
            {
                var primaryAdapter = FindPrimaryAdapter(enhanced, comparison.ScanTarget);
                dbDevice.PrimarySubnet = primaryAdapter?.IPSubnet;
            }

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

        public ObservableCollection<EnhancedComputerInfo> EnhancedComputerResults
        {
            get => _enhancedComputerResults;
            set
            {
                _enhancedComputerResults = value;
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

                // Parse targets from input
                var targets = TargetInput.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(t => t.Trim())
                                         .Where(t => !string.IsNullOrEmpty(t))
                                         .ToList();

                if (!targets.Any())
                {
                    targets.Add(Environment.MachineName);
                    StatusMessage = "No targets specified, scanning local computer...";
                }
                else
                {
                    if (targets.Count == 1)
                    {
                        StatusMessage = $"Scanning {targets.First()}...";
                    }
                    else
                    {
                        StatusMessage = $"Scanning {targets.Count} target(s): {string.Join(", ", targets)}";
                    }
                }

                // Give UI time to update
                await Task.Delay(500);

                // Clear previous results
                ComparisonResults.Clear();
                EnhancedComputerResults.Clear();

                // Process each target
                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    StatusMessage = $"Processing {target} ({i + 1}/{targets.Count})...";
                    await Task.Delay(100);

                    // Get enhanced computer info using new service
                    var enhancedInfo = await ComputerInfoCollectionService.GetComputerInfoAsync(target, true);
                    EnhancedComputerResults.Add(enhancedInfo);

                    // Lookup in database for comparison
                    Models.Device? dbDevice = null;
                    try
                    {
                        dbDevice = await _databaseService.GetDeviceByHostnameAsync(target);
                    }
                    catch (Exception)
                    {
                        // Optionally log or handle DB errors
                    }
                    var comparison = new ComputerInfoComparisonResult(enhancedInfo, dbDevice, target);
                    ComparisonResults.Add(comparison);
                }

                HasResults = EnhancedComputerResults.Count > 0;
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
                // Add error info to enhanced results
                var errorInfo = new EnhancedComputerInfo
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
                    ErrorMessage = "Error occurred during scanning"
                };
                EnhancedComputerResults.Add(errorInfo);
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
            ComparisonResults.Clear();
            EnhancedComputerResults.Clear();
            HasResults = false;
            StatusMessage = "";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Helper method to find primary adapter based on scan target
        private Scripts.EnhancedNetworkAdapter? FindPrimaryAdapter(Scripts.EnhancedComputerInfo enhanced, string scanTarget)
        {
            if (!enhanced.HasNetworkAdapters || string.IsNullOrEmpty(scanTarget))
                return enhanced.NetworkAdapters.FirstOrDefault();

            // Find the adapter that matches our scan input (the one we connected through)
            var matchingAdapter = enhanced.NetworkAdapters.FirstOrDefault(adapter =>
                !string.IsNullOrEmpty(adapter.IPAddress) && 
                adapter.IPAddress.Equals(scanTarget.Trim(), StringComparison.OrdinalIgnoreCase));

            // If no exact match found, try to resolve hostname to IP and match
            if (matchingAdapter == null)
            {
                try
                {
                    // Try to resolve hostname to IP address
                    var hostEntry = System.Net.Dns.GetHostEntry(scanTarget.Trim());
                    var resolvedIPs = hostEntry.AddressList.Select(ip => ip.ToString());
                    
                    matchingAdapter = enhanced.NetworkAdapters.FirstOrDefault(adapter =>
                        !string.IsNullOrEmpty(adapter.IPAddress) && 
                        resolvedIPs.Contains(adapter.IPAddress));
                }
                catch
                {
                    // DNS resolution failed, continue with default behavior
                }
            }

            // Use the matching adapter as primary, or fall back to first adapter
            return matchingAdapter ?? enhanced.NetworkAdapters.FirstOrDefault();
        }

        // Add computer information to database with duplicate detection
        private async Task AddComputerToDatabaseAsync(EnhancedComputerInfo computerInfo)
        {
            if (computerInfo == null || computerInfo.HasError)
            {
                await _dialogService.ShowAlertAsync("Error", "Cannot add computer with errors to database.");
                return;
            }

            try
            {
                StatusMessage = $"Adding {computerInfo.ComputerName} to database...";

                // Convert EnhancedComputerInfo to Device model
                var device = ConvertToDeviceModel(computerInfo);

                // Add device directly without duplicate detection
                var success = await _addDeviceService.AddDeviceAsync(device);

                if (success)
                {
                    StatusMessage = $"Successfully added {computerInfo.ComputerName} to database!";
                    
                    // Refresh comparison results to show database match
                    var updatedComparison = ComparisonResults.FirstOrDefault(c => c.ScanTarget == computerInfo.ComputerName);
                    if (updatedComparison != null)
                    {
                        // Reload database info for comparison
                        var dbDevice = await _databaseService.GetDeviceByHostnameAsync(computerInfo.ComputerName);
                        updatedComparison.DatabaseInfo = dbDevice;
                        
                        // Notify property changes to refresh the UI binding
                        OnPropertyChanged(nameof(ComparisonResults));
                    }
                }
                else
                {
                    StatusMessage = "Failed to add computer to database.";
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Error", $"An error occurred while adding to database: {ex.Message}");
                StatusMessage = "Failed to add computer to database.";
            }
        }

        // Convert EnhancedComputerInfo to Device model
        private Models.Device ConvertToDeviceModel(EnhancedComputerInfo computerInfo)
        {
            var device = new Models.Device
            {
                Hostname = computerInfo.ComputerName,
                SerialNumber = computerInfo.SerialNumber,
                AssetTag = computerInfo.AssetTag,
                Manufacturer = computerInfo.Manufacturer,
                Model = computerInfo.Model,
                BiosVersion = computerInfo.BIOSVersion,
                OsName = computerInfo.OperatingSystem,
                OSVersion = computerInfo.OSVersion,
                OsArchitecture = computerInfo.OSArchitecture,
                OsInstallDate = !string.IsNullOrEmpty(computerInfo.OSInstallDate) && DateTime.TryParse(computerInfo.OSInstallDate, out var osDate) ? osDate : null,
                DeviceStatus = "Active",
                LastDiscovered = DateTime.Now,
                DiscoveryMethod = "Computer Scan",
                device_type = "Computer"
            };

            // Set domain information
            if (computerInfo.ActiveDirectoryInfo != null)
            {
                device.DomainName = computerInfo.ActiveDirectoryInfo.DNSHostName;
                device.IsDomainJoined = computerInfo.ActiveDirectoryInfo != null;
            }

            // Set memory information
            if (computerInfo.PhysicalMemory.Any())
            {
                var totalRamGB = computerInfo.PhysicalMemory
                    .Where(m => !string.IsNullOrEmpty(m.Capacity) && m.Capacity.EndsWith("GB"))
                    .Sum(m => decimal.TryParse(m.Capacity.Replace("GB", ""), out var cap) ? cap : 0);
                
                device.TotalRamGb = totalRamGB;
                device.RamType = computerInfo.PhysicalMemory.FirstOrDefault()?.RAMType;
                device.RamSpeed = computerInfo.PhysicalMemory.FirstOrDefault()?.Speed;
                device.RamManufacturer = computerInfo.PhysicalMemory.FirstOrDefault()?.Manufacturer;
            }

            // Set storage information
            if (computerInfo.PhysicalDisks.Any())
            {
                var primaryDisk = computerInfo.PhysicalDisks.FirstOrDefault();
                if (primaryDisk != null)
                {
                    device.StorageInfo = primaryDisk.DiskCapacity;
                    device.StorageType = primaryDisk.DiskType;
                    device.StorageModel = primaryDisk.DiskModel;
                }

                // Additional drives
                if (computerInfo.PhysicalDisks.Count > 1)
                {
                    var additionalDisks = computerInfo.PhysicalDisks.Skip(1).Take(3).ToList();
                    
                    if (additionalDisks.Count > 0)
                    {
                        device.Drive2Name = additionalDisks[0].DiskName;
                        device.Drive2Capacity = additionalDisks[0].DiskCapacity;
                        device.Drive2Type = additionalDisks[0].DiskType;
                        device.Drive2Model = additionalDisks[0].DiskModel;
                    }
                    
                    if (additionalDisks.Count > 1)
                    {
                        device.Drive3Name = additionalDisks[1].DiskName;
                        device.Drive3Capacity = additionalDisks[1].DiskCapacity;
                        device.Drive3Type = additionalDisks[1].DiskType;
                        device.Drive3Model = additionalDisks[1].DiskModel;
                    }
                    
                    if (additionalDisks.Count > 2)
                    {
                        device.Drive4Name = additionalDisks[2].DiskName;
                        device.Drive4Capacity = additionalDisks[2].DiskCapacity;
                        device.Drive4Type = additionalDisks[2].DiskType;
                        device.Drive4Model = additionalDisks[2].DiskModel;
                    }
                }
            }

            // Set network information
            if (computerInfo.NetworkAdapters.Any())
            {
                var primaryAdapter = computerInfo.NetworkAdapters.FirstOrDefault();
                if (primaryAdapter != null)
                {
                    device.PrimaryIp = primaryAdapter.IPAddress;
                    device.PrimaryMac = primaryAdapter.MACAddress;
                    device.PrimarySubnet = primaryAdapter.IPSubnet;
                    
                    if (!string.IsNullOrEmpty(primaryAdapter.DNSServers))
                    {
                        var dnsServers = primaryAdapter.DNSServers.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (dnsServers.Length > 0) device.PrimaryDns = dnsServers[0].Trim();
                        if (dnsServers.Length > 1) device.SecondaryDns = dnsServers[1].Trim();
                    }
                }

                // Additional network adapters
                var additionalAdapters = computerInfo.NetworkAdapters.Skip(1).Take(3).ToList();
                
                if (additionalAdapters.Count > 0)
                {
                    device.Nic2Name = additionalAdapters[0].AdapterName;
                    device.Nic2Ip = additionalAdapters[0].IPAddress;
                    device.Nic2Mac = additionalAdapters[0].MACAddress;
                    device.Nic2Subnet = additionalAdapters[0].IPSubnet;
                }
                
                if (additionalAdapters.Count > 1)
                {
                    device.Nic3Name = additionalAdapters[1].AdapterName;
                    device.Nic3Ip = additionalAdapters[1].IPAddress;
                    device.Nic3Mac = additionalAdapters[1].MACAddress;
                    device.Nic3Subnet = additionalAdapters[1].IPSubnet;
                }
                
                if (additionalAdapters.Count > 2)
                {
                    device.Nic4Name = additionalAdapters[2].AdapterName;
                    device.Nic4Ip = additionalAdapters[2].IPAddress;
                    device.Nic4Mac = additionalAdapters[2].MACAddress;
                    device.Nic4Subnet = additionalAdapters[2].IPSubnet;
                }
            }

            return device;
        }
    }
}
