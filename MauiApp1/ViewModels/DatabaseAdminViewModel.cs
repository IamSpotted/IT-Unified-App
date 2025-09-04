using MauiApp1.Models;
using MauiApp1.Interfaces;
using MauiApp1.Services;
using MauiApp1.Scripts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MauiApp1.ViewModels;

public partial class DatabaseAdminViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IAddDeviceService _addDeviceService;
    private readonly IUpdateDeviceService _updateDeviceService;
    private readonly IEditDeviceService _editDeviceService;
    private new readonly IDialogService _dialogService;
    private readonly IRemoveDeviceService? _removeDeviceService;
    private readonly IBulkDeviceScanService _bulkDeviceScanService;

    public DatabaseAdminViewModel(
        ILogger<DatabaseAdminViewModel> logger,
        IDatabaseService databaseService,
        IAddDeviceService addDeviceService,
        IUpdateDeviceService updateDeviceService,
        IEditDeviceService editDeviceService,
        INavigationService navigationService,
        IDialogService dialogService,
        IRemoveDeviceService removeDeviceService,
        IBulkDeviceScanService bulkDeviceScanService)
        : base(logger, navigationService, dialogService)
    {
        _databaseService = databaseService;
        _addDeviceService = addDeviceService;
        _updateDeviceService = updateDeviceService;
        _editDeviceService = editDeviceService;
        _dialogService = dialogService;
        _removeDeviceService = removeDeviceService;
        _bulkDeviceScanService = bulkDeviceScanService;
    }

    // Override OnPropertyChanged to make it internal for service access
    internal new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
    }

    [ObservableProperty]
    private string _hostname = string.Empty;

    [ObservableProperty]
    private string _serial_number = string.Empty;

    [ObservableProperty]
    private string _asset_tag = string.Empty;

    [ObservableProperty]
    private string _domain_name = string.Empty;

    [ObservableProperty]
    private string _workgroup = string.Empty;

    [ObservableProperty]
    private bool _is_domain_joined = true;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private string _cpu_info = string.Empty;

    [ObservableProperty]
    private int _total_ram_gb = 0;

    [ObservableProperty]
    private string _ram_type = string.Empty;

    [ObservableProperty]
    private string _ram_speed = string.Empty;

    [ObservableProperty]
    private string _ram_manufacturer = string.Empty;

    [ObservableProperty]
    private string _storage_info = string.Empty;

    [ObservableProperty]
    private string _storage_type = string.Empty;

    [ObservableProperty]
    private string _storage_model = string.Empty;

    [ObservableProperty]
    private string _bios_version = string.Empty;

    [ObservableProperty]
    private string _os_name = string.Empty;

    [ObservableProperty]
    private string _os_version = string.Empty;

    [ObservableProperty]
    private string _os_architecture = string.Empty;

    [ObservableProperty]
    private string _primaryIp = string.Empty;

    [ObservableProperty]
    private string _primaryMac = string.Empty;

    [ObservableProperty]
    private string _secondary_ips = string.Empty;

    [ObservableProperty]
    private string _secondary_macs = string.Empty;

    [ObservableProperty]
    private string _dns_servers = string.Empty;

    [ObservableProperty]
    private string _primary_dns = string.Empty;

    [ObservableProperty]
    private string _secondary_dns = string.Empty;

    // Properties that match XAML binding exactly
    [ObservableProperty]
    private string _primaryDns = string.Empty;

    [ObservableProperty]
    private string _secondaryDns = string.Empty;

    // Audit-related properties
    [ObservableProperty]
    private string _changeReason = string.Empty;

    [ObservableProperty]
    private bool _isChangeReasonRequired = false;

    [ObservableProperty]
    private bool _isChangeReasonVisible = false;

    // Session tracking
    private Guid? _currentDiscoverySessionId = null;

    [ObservableProperty]
    private string _default_gateways = string.Empty;

    [ObservableProperty]
    private string _subnet_masks = string.Empty;

    [ObservableProperty]
    private string _primarySubnet = string.Empty;

    // Additional Network Interfaces (2-4)
    [ObservableProperty]
    private string _nic2Name = string.Empty;

    [ObservableProperty]
    private string _nic2Ip = string.Empty;

    [ObservableProperty]
    private string _nic2Mac = string.Empty;

    [ObservableProperty]
    private string _nic2Subnet = string.Empty;

    [ObservableProperty]
    private string _nic3Name = string.Empty;

    [ObservableProperty]
    private string _nic3Ip = string.Empty;

    [ObservableProperty]
    private string _nic3Mac = string.Empty;

    [ObservableProperty]
    private string _nic3Subnet = string.Empty;

    [ObservableProperty]
    private string _nic4Name = string.Empty;

    [ObservableProperty]
    private string _nic4Ip = string.Empty;

    [ObservableProperty]
    private string _nic4Mac = string.Empty;

    [ObservableProperty]
    private string _nic4Subnet = string.Empty;

    // Additional Storage Drives (2-4)
    [ObservableProperty]
    private string _drive2Name = string.Empty;

    [ObservableProperty]
    private string _drive2Capacity = string.Empty;

    [ObservableProperty]
    private string _drive2Type = string.Empty;

    [ObservableProperty]
    private string _drive2Model = string.Empty;

    [ObservableProperty]
    private string _drive3Name = string.Empty;

    [ObservableProperty]
    private string _drive3Capacity = string.Empty;

    [ObservableProperty]
    private string _drive3Type = string.Empty;

    [ObservableProperty]
    private string _drive3Model = string.Empty;

    [ObservableProperty]
    private string _drive4Name = string.Empty;

    [ObservableProperty]
    private string _drive4Capacity = string.Empty;

    [ObservableProperty]
    private string _drive4Type = string.Empty;

    [ObservableProperty]
    private string _drive4Model = string.Empty;

    // UI Control Properties for Dynamic NICs and Storage
    [ObservableProperty]
    private bool _isNic2Visible = false;

    [ObservableProperty]
    private bool _isNic3Visible = false;

    [ObservableProperty]
    private bool _isNic4Visible = false;

    [ObservableProperty]
    private bool _isDrive2Visible = false;

    [ObservableProperty]
    private bool _isDrive3Visible = false;

    [ObservableProperty]
    private bool _isDrive4Visible = false;

    // Collection-based properties for UI binding
    [ObservableProperty]
    private ObservableCollection<NetworkAdapterItem> _additionalAdapters = new();

    [ObservableProperty]
    private ObservableCollection<StorageDriveItem> _additionalDrives = new();

    // Properties for UI state
    public bool CanAddMoreAdapters => AdditionalAdapters.Count < 3;
    public bool CanAddMoreDrives => AdditionalDrives.Count < 3;
    public bool HasAdditionalAdapters => AdditionalAdapters.Count > 0;
    public bool HasAdditionalDrives => AdditionalDrives.Count > 0;

    [ObservableProperty]
    private string _device_status = string.Empty;

    [ObservableProperty]
    private string _area = string.Empty;

    [ObservableProperty]
    private string _zone = string.Empty;

    [ObservableProperty]
    private string _line = string.Empty;

    [ObservableProperty]
    private string _pitch = string.Empty;

    [ObservableProperty]
    private string _floor = string.Empty;

    [ObservableProperty]
    private string _pillar = string.Empty;

    [ObservableProperty]
    private string _additional_notes = string.Empty;

    [ObservableProperty]
    private DateTime _updated_at = DateTime.Now;

    [ObservableProperty]
    private DateTime _last_discovered = DateTime.Now;

    [ObservableProperty]
    private string _discovery_method = string.Empty;

    [ObservableProperty]
    private string _device_type = string.Empty;

    [ObservableProperty]
    private string _webInterfaceUrl = string.Empty;

    [ObservableProperty]
    private string _equipment_group = string.Empty;

    [ObservableProperty]
    private Models.Device? _selectedDevice = null;

    [ObservableProperty]
    private string _selectedDeviceType = string.Empty;

    // Edit mode properties
    [ObservableProperty]
    private bool _isEditMode = false;

    [ObservableProperty]
    private string _editButtonText = "Edit";

    [ObservableProperty]
    private string _editHostname = string.Empty;
    [ObservableProperty]
    private string _editSerialNumber = string.Empty;
    [ObservableProperty]
    private string _editAssetTag = string.Empty;
    [ObservableProperty]
    private string _editDomainName = string.Empty;
    [ObservableProperty]
    private string _editWorkgroup = string.Empty;
    [ObservableProperty]
    private bool _editIsDomainJoined = true;
    [ObservableProperty]
    private string _editManufacturer = string.Empty;
    [ObservableProperty]
    private string _editModel = string.Empty;
    [ObservableProperty]
    private string _editCpuInfo = string.Empty;
    [ObservableProperty]
    private int _editTotalRamGb = 0;
    [ObservableProperty]
    private string _editRamType = string.Empty;
    [ObservableProperty]
    private string _editStorageInfo = string.Empty;
    [ObservableProperty]
    private string _editBiosVersion = string.Empty;
    [ObservableProperty]
    private string _editOsName = string.Empty;
    [ObservableProperty]
    private string _editOSVersion = string.Empty;
    [ObservableProperty]
    private string _editOsArchitecture = string.Empty;
    [ObservableProperty]
    private string _editPrimaryIp = string.Empty;
    [ObservableProperty]
    private string _editPrimaryMac = string.Empty;
    
    [ObservableProperty]
    private string _editPrimarySubnet = string.Empty;
    
    [ObservableProperty]
    private string _editPrimaryDns = string.Empty;
    
    [ObservableProperty]
    private string _editSecondaryDns = string.Empty;
    
    // RAM fields to match add device form
    [ObservableProperty]
    private string _editRamSpeed = string.Empty;
    
    [ObservableProperty]
    private string _editRamManufacturer = string.Empty;
    
    // Storage fields to match add device form
    [ObservableProperty]
    private string _editStorageType = string.Empty;
    
    [ObservableProperty]
    private string _editStorageModel = string.Empty;
    [ObservableProperty]
    private string _editDeviceStatus = string.Empty;
    [ObservableProperty]
    private string _editArea = string.Empty;
    [ObservableProperty]
    private string _editZone = string.Empty;
    [ObservableProperty]
    private string _editLine = string.Empty;
    [ObservableProperty]
    private string _editPitch = string.Empty;
    [ObservableProperty]
    private string _editFloor = string.Empty;
    [ObservableProperty]
    private string _editPillar = string.Empty;
    [ObservableProperty]
    private string _editAdditionalNotes = string.Empty;
    [ObservableProperty]
    private DateTime _editUpdatedAt = DateTime.Now;
    [ObservableProperty]
    private DateTime _editLastDiscovered = DateTime.Now;
    [ObservableProperty]
    private string _editDiscoveryMethod = string.Empty;
    [ObservableProperty]
    private string _editDeviceType = string.Empty;
    [ObservableProperty]
    private string _editWebLink = string.Empty;
    [ObservableProperty]
    private string _editEquipmentGroup = string.Empty;

    // Edit NIC properties to match add device form
    [ObservableProperty]
    private string _editNic2Name = string.Empty;
    
    [ObservableProperty]
    private string _editNic2Ip = string.Empty;
    
    [ObservableProperty]
    private string _editNic2Mac = string.Empty;
    
    [ObservableProperty]
    private string _editNic2Subnet = string.Empty;
    
    [ObservableProperty]
    private string _editNic3Name = string.Empty;
    
    [ObservableProperty]
    private string _editNic3Ip = string.Empty;
    
    [ObservableProperty]
    private string _editNic3Mac = string.Empty;
    
    [ObservableProperty]
    private string _editNic3Subnet = string.Empty;
    
    [ObservableProperty]
    private string _editNic4Name = string.Empty;
    
    [ObservableProperty]
    private string _editNic4Ip = string.Empty;
    
    [ObservableProperty]
    private string _editNic4Mac = string.Empty;
    
    [ObservableProperty]
    private string _editNic4Subnet = string.Empty;

    // Available options for dropdowns
    public List<string> device_type_options { get; } = InputSanitizer.DeviceTypeOptions.ToList();
    public List<string> device_status_options { get; } = InputSanitizer.DeviceStatusOptions.ToList();

    // Search Properties
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching = false;

    [ObservableProperty]
    private ObservableCollection<Models.Device> _searchResults = new();

    [ObservableProperty]
    private bool _hasSearchResults = false;

    // Status Properties
    [ObservableProperty]
    private bool _isStatusVisible = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Scan Device Properties
    [ObservableProperty]
    private string _scanDeviceInput = string.Empty;

    [ObservableProperty]
    private bool _isScanning = false;

    [ObservableProperty]
    private bool _isScanStatusVisible = false;

    [ObservableProperty]
    private string _scanStatusMessage = string.Empty;

    // Bulk Device Scan Properties
    [ObservableProperty]
    private string _bulkScanFilePath = string.Empty;

    [ObservableProperty]
    private string _bulkScanChangeReason = string.Empty;

    [ObservableProperty]
    private bool _isBulkScanning = false;

    [ObservableProperty]
    private bool _isBulkScanStatusVisible = false;

    [ObservableProperty]
    private string _bulkScanStatusMessage = string.Empty;

    [ObservableProperty]
    private string _bulkScanResultsSummary = string.Empty;

    [ObservableProperty]
    private bool _hasBulkScanResults = false;

    [ObservableProperty]
    private int _bulkScanProgress = 0;

    private Models.BulkScanResult? _lastBulkScanResult;

    // Computed properties for bulk scan
    public bool IsBulkScanChangeReasonRequired => true; // Always require reason for bulk operations
    public bool CanStartBulkScan => !string.IsNullOrWhiteSpace(BulkScanFilePath) && 
                                   !string.IsNullOrWhiteSpace(BulkScanChangeReason) && 
                                   !IsBulkScanning;

    public DatabaseAdminViewModel(
        ILogger<DatabaseAdminViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        IDatabaseService databaseService,
        IAddDeviceService addDeviceService,
        IUpdateDeviceService updateDeviceService,
        IBulkDeviceScanService bulkDeviceScanService)
        : base(logger, navigationService, dialogService)
    {
        Title = "Database Admin";
        _dialogService = dialogService;
        _databaseService = databaseService;
        _addDeviceService = addDeviceService;
        _updateDeviceService = updateDeviceService;
        _bulkDeviceScanService = bulkDeviceScanService;

        // Load existing devices
        _ = LoadAllDevicesCommand.ExecuteAsync(null);
    }

    [ObservableProperty]
    private ObservableCollection<Models.Device> _devices = new();
    [RelayCommand]
    private async Task LoadAllDevices()
    {
        await ExecuteSafelyAsync(async () =>
        {
            try
            {
                var devices = await _databaseService.GetDevicesAsync();
                Devices = new ObservableCollection<Models.Device>(devices);

                _logger.LogInformation("Loaded {Count} devices from the database", Devices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load devices from the database");
                await _dialogService.ShowAlertAsync("Error", "Failed to load devices from the database. Please check your connection.");
            }
        }, "Load All Devices");
    }

    public IAsyncRelayCommand LoadDataCommand => LoadAllDevicesCommand;

    [RelayCommand]
    private async Task AddDevice()
    {
        IsStatusVisible = true;
        StatusMessage = "Adding device...";

        await ExecuteSafelyAsync(async () =>
        {
            try
            {
                ValidateAndSanitizeInput();

                // Validate change reason if required
                if (IsChangeReasonRequired && string.IsNullOrWhiteSpace(ChangeReason))
                {
                    await _dialogService.ShowAlertAsync("Validation Error", "Change reason is required when editing devices.");
                    return;
                }

                var device = new Models.Device
                {
                    Hostname = Hostname,
                    SerialNumber = Serial_number,
                    AssetTag = Asset_tag,
                    DomainName = Domain_name,
                    Workgroup = Workgroup,
                    IsDomainJoined = Is_domain_joined,
                    Manufacturer = Manufacturer,
                    Model = Model,
                    CpuInfo = Cpu_info,
                    TotalRamGb = Total_ram_gb,
                    RamType = Ram_type,
                    StorageInfo = Storage_info,
                    BiosVersion = Bios_version,
                    OsName = Os_name,
                    OSVersion = Os_version,
                    OsArchitecture = Os_architecture,
                    PrimaryIp = PrimaryIp,
                    PrimaryMac = PrimaryMac,
                    PrimarySubnet = Subnet_masks,
                    PrimaryDns = Dns_servers,
                    SecondaryDns = string.Empty, // Can be expanded later
                    
                    // Additional NICs
                    Nic2Name = IsNic2Visible ? Nic2Name : null,
                    Nic2Ip = IsNic2Visible ? Nic2Ip : null,
                    Nic2Mac = IsNic2Visible ? Nic2Mac : null,
                    Nic2Subnet = IsNic2Visible ? Nic2Subnet : null,
                    
                    Nic3Name = IsNic3Visible ? Nic3Name : null,
                    Nic3Ip = IsNic3Visible ? Nic3Ip : null,
                    Nic3Mac = IsNic3Visible ? Nic3Mac : null,
                    Nic3Subnet = IsNic3Visible ? Nic3Subnet : null,
                    
                    Nic4Name = IsNic4Visible ? Nic4Name : null,
                    Nic4Ip = IsNic4Visible ? Nic4Ip : null,
                    Nic4Mac = IsNic4Visible ? Nic4Mac : null,
                    Nic4Subnet = IsNic4Visible ? Nic4Subnet : null,
                    
                    // Additional Storage Drives
                    Drive2Name = IsDrive2Visible ? Drive2Name : null,
                    Drive2Capacity = IsDrive2Visible ? Drive2Capacity : null,
                    Drive2Type = IsDrive2Visible ? Drive2Type : null,
                    Drive2Model = IsDrive2Visible ? Drive2Model : null,
                    
                    Drive3Name = IsDrive3Visible ? Drive3Name : null,
                    Drive3Capacity = IsDrive3Visible ? Drive3Capacity : null,
                    Drive3Type = IsDrive3Visible ? Drive3Type : null,
                    Drive3Model = IsDrive3Visible ? Drive3Model : null,
                    
                    Drive4Name = IsDrive4Visible ? Drive4Name : null,
                    Drive4Capacity = IsDrive4Visible ? Drive4Capacity : null,
                    Drive4Type = IsDrive4Visible ? Drive4Type : null,
                    Drive4Model = IsDrive4Visible ? Drive4Model : null,
                    
                    DeviceStatus = Device_status,
                    Area = Area,
                    Zone = Zone,
                    Line = Line,
                    Pitch = Pitch,
                    Floor = Floor,
                    Pillar = Pillar,
                    AdditionalNotes = Additional_notes,
                    UpdatedAt = DateTime.Now,
                    LastDiscovered = DateTime.Now,
                    DiscoveryMethod = Discovery_method,
                    device_type = SelectedDeviceType,
                    WebInterfaceUrl = WebInterfaceUrl,
                    EquipmentGroup = Equipment_group
                };

                // Use the enhanced AddDeviceAsync method with audit logging
                var success = await _addDeviceService.AddDeviceAsync(
                    device, 
                    SelectedDeviceType,
                    GetApplicationUser(),
                    _currentDiscoverySessionId,
                    ChangeReason
                );

                if (success)
                {
                    StatusMessage = $"Device '{Hostname}' added successfully to database!";
                    _logger.LogInformation("Added new device: {Hostname} at {PrimaryIp}", Hostname, PrimaryIp);
                }
                else
                {
                    StatusMessage = $"Failed to add device '{Hostname}' to database. Please try again.";
                    _logger.LogError("Failed to add device: {Hostname} at {PrimaryIp}", Hostname, PrimaryIp);
                }

                // Clear form after successful addition
                ClearForm();
                // Reload devices to show the new addition
                await LoadAllDevicesCommand.ExecuteAsync(null);
            }
            catch (ArgumentException ex)
            {
                StatusMessage = $"Input Error: {ex.Message}";
                await _dialogService.ShowAlertAsync("Input Error", $"Invalid input detected: {ex.Message}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unexpected Error: {ex.Message}";
                await _dialogService.ShowAlertAsync("Unexpected Error", $"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                await Task.Delay(3000);
                IsStatusVisible = false;
            }
        }, "Add Device");
    }

    [RelayCommand]
    private async Task DeleteDevice(Models.Device device)
    {
        if (device == null)
        {
            await _dialogService.ShowAlertAsync("Delete Error", "No device selected for deletion.");
            return;
        }

        if (_removeDeviceService == null)
        {
            await _dialogService.ShowAlertAsync("Delete Error", "RemoveDeviceService is not available.");
            return;
        }
        
        var confirm = await _dialogService.ShowConfirmationAsync("Confirm Delete", $"Are you sure you want to delete {device.Hostname}?");
        if (!confirm)
            return;

        // Prompt for deletion reason
        var deletionReason = await _dialogService.ShowPromptAsync(
            "Deletion Reason", 
            "Please provide a reason for deleting this device:", 
            "Enter reason...",
            $"Deletion of {device.Hostname} requested by user");

        if (string.IsNullOrWhiteSpace(deletionReason))
        {
            await _dialogService.ShowAlertAsync("Delete Cancelled", "A deletion reason is required.");
            return;
        }

        var success = await _removeDeviceService.RemoveDeviceAsync(device.Hostname, deletionReason);
        if (success)
        {
            await _dialogService.ShowAlertAsync("Delete Success", $"{device.Hostname} has been deleted and archived.");
            Devices.Remove(device);
            SelectedDevice = null;
        }
        else
        {
            await _dialogService.ShowAlertAsync("Delete Error", "Failed to delete device.");
        }
    }

    [RelayCommand]
    private void ResetForm()
    {
        ClearForm();
    }

    private Guid GenerateDiscoverySessionId()
    {
        if (_currentDiscoverySessionId == null)
        {
            _currentDiscoverySessionId = Guid.NewGuid();
        }
        return _currentDiscoverySessionId.Value;
    }

    private string GetApplicationUser()
    {
        return Environment.UserName; // Gets current Windows username
    }

    internal void SetChangeReasonForMode(bool isEditMode)
    {
        IsChangeReasonVisible = true;
        
        if (isEditMode)
        {
            IsChangeReasonRequired = true;
            ChangeReason = string.Empty; // Clear any previous reason
        }
        else
        {
            IsChangeReasonRequired = false;
            ChangeReason = "New device added via scan"; // Auto-populate for new devices
        }
    }

    internal void ClearForm()
    {
        Hostname = string.Empty;
        Serial_number = string.Empty;
        Asset_tag = string.Empty;
        Domain_name = string.Empty;
        Workgroup = string.Empty;
        Is_domain_joined = true;
        Manufacturer = string.Empty;
        Model = string.Empty;
        Cpu_info = string.Empty;
        Total_ram_gb = 0;
        Ram_type = string.Empty;
        Ram_speed = string.Empty;
        Ram_manufacturer = string.Empty;
        Storage_info = string.Empty;
        Storage_type = string.Empty;
        Storage_model = string.Empty;
        Bios_version = string.Empty;
        Os_name = string.Empty;
        Os_version = string.Empty;
        Os_architecture = string.Empty;
        PrimaryIp = string.Empty;
        PrimaryMac = string.Empty;
        Secondary_ips = string.Empty;
        Secondary_macs = string.Empty;
        Dns_servers = string.Empty;
        Primary_dns = string.Empty;
        Secondary_dns = string.Empty;
        PrimaryDns = string.Empty;
        SecondaryDns = string.Empty;
        Default_gateways = string.Empty;
        Subnet_masks = string.Empty;
        PrimarySubnet = string.Empty;
        Device_status = string.Empty;
        Area = string.Empty;
        Zone = string.Empty;
        Line = string.Empty;
        Pitch = string.Empty;
        Floor = string.Empty;
        Pillar = string.Empty;
        Additional_notes = string.Empty;
        Updated_at = DateTime.Now;
        Last_discovered = DateTime.Now;
        Discovery_method = string.Empty;
        Device_type = string.Empty;
        WebInterfaceUrl = string.Empty;
        Equipment_group = string.Empty;
        
        // Clear additional NICs and hide them
        ClearNic2();
        ClearNic3();
        ClearNic4();
        IsNic2Visible = false;
        IsNic3Visible = false;
        IsNic4Visible = false;
        
        // Clear additional storage and hide them
        ClearDrive2();
        ClearDrive3();
        ClearDrive4();
        IsDrive2Visible = false;
        IsDrive3Visible = false;
        IsDrive4Visible = false;
        
        // Clear collections
        AdditionalAdapters.Clear();
        AdditionalDrives.Clear();
        OnPropertyChanged(nameof(CanAddMoreAdapters));
        OnPropertyChanged(nameof(HasAdditionalAdapters));
        OnPropertyChanged(nameof(CanAddMoreDrives));
        OnPropertyChanged(nameof(HasAdditionalDrives));

        // Reset audit tracking
        _currentDiscoverySessionId = null;
        ChangeReason = string.Empty;
        IsChangeReasonRequired = false;
        IsChangeReasonVisible = false;
        
        SelectedDevice = null;
        IsEditMode = false;
        EditButtonText = "Edit";
    }

    [RelayCommand]
    private async Task ToggleEditMode()
    {
        if (IsEditMode)
        {
            // Exit edit mode using the new service
            await _editDeviceService.ClearEditFormAsync(this);
        }
        else
        {
            // This shouldn't happen in normal flow, but handle it gracefully
            await _editDeviceService.SetEditModeAsync(this, true, null);
        }
    }

    // Search Methods
    [RelayCommand]
    public async Task SearchDevices()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await _dialogService.ShowAlertAsync("Search Error", "Please enter a search term.");
            return;
        }

        IsSearching = true;
        try
        {
            var devices = await _databaseService.SearchDevicesAsync(SearchQuery.Trim());
            SearchResults.Clear();
            foreach (var device in devices)
            {
                SearchResults.Add(device);
            }
            HasSearchResults = SearchResults.Count > 0;
            
            if (!HasSearchResults)
            {
                await _dialogService.ShowAlertAsync("Search Results", "No devices found matching your search criteria.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Search Error", $"An error occurred while searching: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        HasSearchResults = false;
    }

    [RelayCommand]
    public async Task ViewDevice(Models.Device device)
    {
        if (device == null) return;

        try
        {
            // Navigate to the device details popup
            var parameters = new Dictionary<string, object>
            {
                { "Device", device }
            };

            await _navigationService.NavigateToAsync(nameof(Views.DeviceDetailsPopup), parameters);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Navigation Error", $"Unable to show device details: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task EditSearchedDevice(Models.Device device)
    {
        if (device == null || string.IsNullOrWhiteSpace(device.Hostname))
        {
            await _dialogService.ShowAlertAsync("Edit Error", "Invalid device selected for editing.");
            return;
        }

        try
        {
            // Fetch fresh data from database using hostname
            var freshDevice = await _databaseService.GetDeviceByHostnameAsync(device.Hostname);
            
            if (freshDevice == null)
            {
                await _dialogService.ShowAlertAsync("Edit Error", $"Device '{device.Hostname}' was not found in the database. It may have been deleted.");
                return;
            }

            // Populate form with fresh data
            await _editDeviceService.PopulateFormForEditAsync(freshDevice, this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch fresh device data for editing: {Hostname}", device.Hostname);
            await _dialogService.ShowAlertAsync("Edit Error", $"Failed to retrieve current device information: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ScanDevice()
    {
        if (string.IsNullOrWhiteSpace(ScanDeviceInput))
        {
            await _dialogService.ShowAlertAsync("Scan Error", "Please enter a hostname or IP address to scan.");
            return;
        }

        IsScanning = true;
        IsScanStatusVisible = true;
        ScanStatusMessage = $"Scanning {ScanDeviceInput}...";

        try
        {
            // Use the same enhanced computer info service as ComputerInfoViewModel
            var enhancedInfo = await ComputerInfoCollectionService.GetComputerInfoAsync(ScanDeviceInput, true);

            if (enhancedInfo != null && !string.IsNullOrEmpty(enhancedInfo.ComputerName))
            {
                // Auto-populate the form fields with the scanned information
                PopulateFieldsFromScanResult(enhancedInfo);
                ScanStatusMessage = $"Successfully scanned {enhancedInfo.ComputerName}!";
                
                // Hide status after a delay
                await Task.Delay(3000);
                IsScanStatusVisible = false;
            }
            else
            {
                ScanStatusMessage = $"Failed to get information from {ScanDeviceInput}";
                await _dialogService.ShowAlertAsync("Scan Failed", $"Could not retrieve information from {ScanDeviceInput}. Please verify the hostname/IP is correct and accessible.");
                await Task.Delay(3000);
                IsScanStatusVisible = false;
            }
        }
        catch (Exception ex)
        {
            ScanStatusMessage = "Scan failed with error";
            await _dialogService.ShowAlertAsync("Scan Error", $"An error occurred while scanning: {ex.Message}");
            await Task.Delay(3000);
            IsScanStatusVisible = false;
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void PopulateFieldsFromScanResult(EnhancedComputerInfo enhancedInfo)
    {
        // Populate basic computer information
        if (!string.IsNullOrEmpty(enhancedInfo.ComputerName))
            Hostname = enhancedInfo.ComputerName;

        if (!string.IsNullOrEmpty(enhancedInfo.SerialNumber))
            Serial_number = enhancedInfo.SerialNumber;

        if (!string.IsNullOrEmpty(enhancedInfo.AssetTag))
            Asset_tag = enhancedInfo.AssetTag;

        // Populate domain/workgroup information
        if (enhancedInfo.ActiveDirectoryInfo != null)
        {
            if (!string.IsNullOrEmpty(enhancedInfo.ActiveDirectoryInfo.DNSHostName))
                Domain_name = enhancedInfo.ActiveDirectoryInfo.DNSHostName;
            
            Is_domain_joined = true;
        }
        else
        {
            Is_domain_joined = false;
            // Try to get workgroup info if available
            if (!string.IsNullOrEmpty(enhancedInfo.ComputerName))
                Workgroup = "WORKGROUP"; // Default workgroup name
        }

        // Populate hardware information
        if (!string.IsNullOrEmpty(enhancedInfo.Manufacturer))
            Manufacturer = enhancedInfo.Manufacturer;

        if (!string.IsNullOrEmpty(enhancedInfo.Model))
            Model = enhancedInfo.Model;

        // Populate BIOS information
        if (!string.IsNullOrEmpty(enhancedInfo.BIOSVersion))
            Bios_version = enhancedInfo.BIOSVersion;

        // Populate processor information
        if (!string.IsNullOrEmpty(enhancedInfo.ProcessorName))
            Cpu_info = enhancedInfo.ProcessorName;

        // Populate OS information
        if (!string.IsNullOrEmpty(enhancedInfo.OperatingSystem))
            Os_name = enhancedInfo.OperatingSystem;

        if (!string.IsNullOrEmpty(enhancedInfo.OSVersion))
            Os_version = enhancedInfo.OSVersion;

        if (!string.IsNullOrEmpty(enhancedInfo.OSArchitecture))
            Os_architecture = enhancedInfo.OSArchitecture;

        // Populate memory information - use computed property
        if (!string.IsNullOrEmpty(enhancedInfo.RAMInstalled) && enhancedInfo.RAMInstalled != "N/A")
        {
            var ramValue = enhancedInfo.RAMInstalled.Replace(" GB", "").Trim();
            if (decimal.TryParse(ramValue, out var ramGb))
                Total_ram_gb = (int)Math.Round(ramGb);
        }

        // Populate RAM Type from first memory module
        if (enhancedInfo.PhysicalMemory?.Any() == true)
        {
            var firstMemory = enhancedInfo.PhysicalMemory.First();
            if (!string.IsNullOrEmpty(firstMemory.RAMType))
                Ram_type = firstMemory.RAMType;
            if (!string.IsNullOrEmpty(firstMemory.Speed) && firstMemory.Speed != "N/A")
                Ram_speed = firstMemory.Speed;
            if (!string.IsNullOrEmpty(firstMemory.Manufacturer) && firstMemory.Manufacturer != "N/A")
                Ram_manufacturer = firstMemory.Manufacturer;
        }

        // Populate storage information from physical disks
        if (enhancedInfo.PhysicalDisks?.Any() == true)
        {
            var primaryDisk = enhancedInfo.PhysicalDisks.First();
            Storage_info = $"{primaryDisk.DiskName} ({primaryDisk.DiskCapacity})";
            Storage_type = primaryDisk.DiskType;
            Storage_model = primaryDisk.DiskModel;
        }

        // Populate network information from network adapters
        var connectedAdapters = enhancedInfo.NetworkAdapters?
            .Where(a => !string.IsNullOrEmpty(a.IPAddress) && 
                       a.IPAddress != "N/A" && 
                       a.IPAddress != "Not configured" &&
                       (a.NetConnectionStatus == "Connected" || a.NetConnectionStatus == "2"))
            .ToList() ?? new List<EnhancedNetworkAdapter>();

        // Smart primary IP detection - determine which NIC received the communication
        var primaryAdapter = DeterminePrimaryAdapter(connectedAdapters, ScanDeviceInput);

        // Primary network adapter
        if (primaryAdapter != null)
        {
            if (!string.IsNullOrEmpty(primaryAdapter.IPAddress))
                PrimaryIp = primaryAdapter.IPAddress;

            if (!string.IsNullOrEmpty(primaryAdapter.MACAddress))
                PrimaryMac = primaryAdapter.FormattedMACAddress; // Use formatted MAC

            if (!string.IsNullOrEmpty(primaryAdapter.IPSubnet))
            {
                Subnet_masks = primaryAdapter.IPSubnet;
                PrimarySubnet = primaryAdapter.IPSubnet; // For XAML binding
            }

            // Parse DNS servers - split by pipe if multiple
            if (!string.IsNullOrEmpty(primaryAdapter.DNSServers))
            {
                var dnsServers = primaryAdapter.DNSServers.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (dnsServers.Length > 0)
                {
                    Primary_dns = dnsServers[0].Trim();
                    PrimaryDns = dnsServers[0].Trim(); // For XAML binding
                }
                if (dnsServers.Length > 1)
                {
                    Secondary_dns = dnsServers[1].Trim();
                    SecondaryDns = dnsServers[1].Trim(); // For XAML binding
                }
                    
                // Also set the combined DNS servers field
                Dns_servers = primaryAdapter.DNSServers;
            }

            if (!string.IsNullOrEmpty(primaryAdapter.DefaultGateway))
                Default_gateways = primaryAdapter.DefaultGateway;
        }

        // Auto-populate up to 4 adapters total
        var remainingAdapters = connectedAdapters.Where(a => a != primaryAdapter).ToList();
        
        // Clear existing additional adapters
        AdditionalAdapters.Clear();
        
        if (remainingAdapters.Count > 0)
        {
            for (int i = 0; i < Math.Min(remainingAdapters.Count, 3); i++)
            {
                var adapter = remainingAdapters[i];
                var adapterItem = new NetworkAdapterItem
                {
                    Name = adapter.AdapterName,
                    IpAddress = adapter.IPAddress,
                    MacAddress = adapter.FormattedMACAddress,
                    Subnet = adapter.IPSubnet,
                    SubnetMask = adapter.IPSubnet
                };
                
                AdditionalAdapters.Add(adapterItem);
                
                // Also populate individual properties for backward compatibility with Device model
                switch (i)
                {
                    case 0: // NIC 2
                        IsNic2Visible = true;
                        Nic2Name = adapter.AdapterName;
                        Nic2Ip = adapter.IPAddress;
                        Nic2Mac = adapter.FormattedMACAddress;
                        Nic2Subnet = adapter.IPSubnet;
                        break;
                    case 1: // NIC 3
                        IsNic3Visible = true;
                        Nic3Name = adapter.AdapterName;
                        Nic3Ip = adapter.IPAddress;
                        Nic3Mac = adapter.FormattedMACAddress;
                        Nic3Subnet = adapter.IPSubnet;
                        break;
                    case 2: // NIC 4
                        IsNic4Visible = true;
                        Nic4Name = adapter.AdapterName;
                        Nic4Ip = adapter.IPAddress;
                        Nic4Mac = adapter.FormattedMACAddress;
                        Nic4Subnet = adapter.IPSubnet;
                        break;
                }
            }
            
            OnPropertyChanged(nameof(CanAddMoreAdapters));
            OnPropertyChanged(nameof(HasAdditionalAdapters));
        }

        // Auto-populate up to 4 storage drives using physical disks
        if (enhancedInfo.PhysicalDisks?.Any() == true)
        {
            var validDisks = enhancedInfo.PhysicalDisks
                .Where(disk => !string.IsNullOrEmpty(disk.DiskName))
                .ToList();

            // Clear existing additional drives
            AdditionalDrives.Clear();

            // Primary storage (already populated above in Storage_info)
            
            // Additional storage drives (skip first one as it's the primary)
            for (int i = 1; i < Math.Min(validDisks.Count, 4); i++)
            {
                var disk = validDisks[i];
                var driveItem = new StorageDriveItem
                {
                    Name = disk.DiskName,
                    Capacity = disk.DiskCapacity,
                    Type = disk.DiskType,
                    Model = disk.DiskModel
                };
                
                AdditionalDrives.Add(driveItem);
                
                // Also populate individual properties for backward compatibility with Device model
                switch (i)
                {
                    case 1: // Drive 2
                        IsDrive2Visible = true;
                        Drive2Name = disk.DiskName;
                        Drive2Capacity = disk.DiskCapacity;
                        Drive2Type = disk.DiskType;
                        Drive2Model = disk.DiskModel;
                        break;
                        
                    case 2: // Drive 3
                        IsDrive3Visible = true;
                        Drive3Name = disk.DiskName;
                        Drive3Capacity = disk.DiskCapacity;
                        Drive3Type = disk.DiskType;
                        Drive3Model = disk.DiskModel;
                        break;
                        
                    case 3: // Drive 4
                        IsDrive4Visible = true;
                        Drive4Name = disk.DiskName;
                        Drive4Capacity = disk.DiskCapacity;
                        Drive4Type = disk.DiskType;
                        Drive4Model = disk.DiskModel;
                        break;
                }
            }
            
            OnPropertyChanged(nameof(CanAddMoreDrives));
            OnPropertyChanged(nameof(HasAdditionalDrives));
        }

        // Set discovery method
        Discovery_method = "Enhanced Scan";
        
        // Set device type if not already set
        if (string.IsNullOrEmpty(SelectedDeviceType))
            SelectedDeviceType = "Computer";

        // Configure for new device mode with session tracking
        _currentDiscoverySessionId = GenerateDiscoverySessionId();
        SetChangeReasonForMode(isEditMode: false);
    }
    
    private void ValidateAndSanitizeInput()
    {
        // Sanitize string fields
        Hostname = InputSanitizer.SanitizeHostname(Hostname);
        Area = InputSanitizer.SanitizeAlphanumeric(Area);
        Zone = InputSanitizer.SanitizeAlphanumeric(Zone);
        Line = InputSanitizer.SanitizeAlphanumeric(Line);
        Pillar = InputSanitizer.SanitizeAlphanumeric(Pillar);
        Floor = InputSanitizer.SanitizeAlphanumeric(Floor);
        Pitch = InputSanitizer.SanitizeAlphanumeric(Pitch);
        Equipment_group = InputSanitizer.SanitizeAlphanumeric(Equipment_group);
        Asset_tag = InputSanitizer.SanitizeAlphanumeric(Asset_tag);
        Manufacturer = InputSanitizer.SanitizeString(Manufacturer);
        Model = InputSanitizer.SanitizeString(Model);
        Serial_number = InputSanitizer.SanitizeAlphanumeric(Serial_number);
        Additional_notes = InputSanitizer.SanitizeNotes(Additional_notes) ?? string.Empty;
        WebInterfaceUrl = InputSanitizer.SanitizeUrl(WebInterfaceUrl) ?? string.Empty;

        // Validate IP address if provided
        if (!string.IsNullOrEmpty(PrimaryIp))
        {
            PrimaryIp = InputSanitizer.SanitizeIPAddress(PrimaryIp) ?? string.Empty;
        }

        // Validate device type
        SelectedDeviceType = InputSanitizer.SanitizeDeviceType(SelectedDeviceType);
    }

    // Add/Remove NIC Commands (matching XAML binding names)
    [RelayCommand]
    private void AddNetworkAdapter()
    {
        if (AdditionalAdapters.Count < 3)
        {
            AdditionalAdapters.Add(new NetworkAdapterItem());
            OnPropertyChanged(nameof(CanAddMoreAdapters));
            OnPropertyChanged(nameof(HasAdditionalAdapters));
            
            // Also set the visibility flags for backward compatibility
            switch (AdditionalAdapters.Count)
            {
                case 1: IsNic2Visible = true; break;
                case 2: IsNic3Visible = true; break;
                case 3: IsNic4Visible = true; break;
            }
        }
    }

    [RelayCommand]
    private void RemoveNetworkAdapter()
    {
        if (AdditionalAdapters.Count > 0)
        {
            var lastIndex = AdditionalAdapters.Count - 1;
            AdditionalAdapters.RemoveAt(lastIndex);
            OnPropertyChanged(nameof(CanAddMoreAdapters));
            OnPropertyChanged(nameof(HasAdditionalAdapters));
            
            // Also clear the visibility flags for backward compatibility
            switch (lastIndex)
            {
                case 0: IsNic2Visible = false; ClearNic2(); break;
                case 1: IsNic3Visible = false; ClearNic3(); break;
                case 2: IsNic4Visible = false; ClearNic4(); break;
            }
        }
    }

    // Add/Remove Storage Commands (matching XAML binding names)
    [RelayCommand]
    private void AddStorageDrive()
    {
        if (AdditionalDrives.Count < 3)
        {
            AdditionalDrives.Add(new StorageDriveItem());
            OnPropertyChanged(nameof(CanAddMoreDrives));
            OnPropertyChanged(nameof(HasAdditionalDrives));
            
            // Also set the visibility flags for backward compatibility
            switch (AdditionalDrives.Count)
            {
                case 1: IsDrive2Visible = true; break;
                case 2: IsDrive3Visible = true; break;
                case 3: IsDrive4Visible = true; break;
            }
        }
    }

    [RelayCommand]
    private void RemoveStorageDrive()
    {
        if (AdditionalDrives.Count > 0)
        {
            var lastIndex = AdditionalDrives.Count - 1;
            AdditionalDrives.RemoveAt(lastIndex);
            OnPropertyChanged(nameof(CanAddMoreDrives));
            OnPropertyChanged(nameof(HasAdditionalDrives));
            
            // Also clear the visibility flags for backward compatibility
            switch (lastIndex)
            {
                case 0: IsDrive2Visible = false; ClearDrive2(); break;
                case 1: IsDrive3Visible = false; ClearDrive3(); break;
                case 2: IsDrive4Visible = false; ClearDrive4(); break;
            }
        }
    }

    // Helper methods to clear NIC data
    private void ClearNic2()
    {
        Nic2Name = string.Empty;
        Nic2Ip = string.Empty;
        Nic2Mac = string.Empty;
        Nic2Subnet = string.Empty;
    }

    private void ClearNic3()
    {
        Nic3Name = string.Empty;
        Nic3Ip = string.Empty;
        Nic3Mac = string.Empty;
        Nic3Subnet = string.Empty;
    }

    private void ClearNic4()
    {
        Nic4Name = string.Empty;
        Nic4Ip = string.Empty;
        Nic4Mac = string.Empty;
        Nic4Subnet = string.Empty;
    }

    // Helper methods to clear Storage data
    private void ClearDrive2()
    {
        Drive2Name = string.Empty;
        Drive2Capacity = string.Empty;
        Drive2Type = string.Empty;
        Drive2Model = string.Empty;
    }

    private void ClearDrive3()
    {
        Drive3Name = string.Empty;
        Drive3Capacity = string.Empty;
        Drive3Type = string.Empty;
        Drive3Model = string.Empty;
    }

    private void ClearDrive4()
    {
        Drive4Name = string.Empty;
        Drive4Capacity = string.Empty;
        Drive4Type = string.Empty;
        Drive4Model = string.Empty;
    }

    // Smart primary adapter detection based on communication source
    private EnhancedNetworkAdapter? DeterminePrimaryAdapter(List<EnhancedNetworkAdapter> adapters, string scanTarget)
    {
        if (!adapters.Any()) return null;

        // If we scanned by IP address, try to find the adapter with that IP
        if (System.Net.IPAddress.TryParse(scanTarget, out var targetIp))
        {
            var matchingAdapter = adapters.FirstOrDefault(a => a.IPAddress == scanTarget);
            if (matchingAdapter != null)
                return matchingAdapter;
        }

        // If we scanned by hostname, try to determine which adapter would handle the communication
        // This is a best-effort approach since we can't definitively know without actual network tracing
        
        // Priority 1: Adapter with default gateway (most likely to handle external communication)
        var adapterWithGateway = adapters
            .Where(a => !string.IsNullOrEmpty(a.DefaultGateway) && a.DefaultGateway != "0.0.0.0")
            .OrderBy(a => a.IsWireless ? 1 : 0) // Prefer wired over wireless
            .FirstOrDefault();
        
        if (adapterWithGateway != null)
            return adapterWithGateway;

        // Priority 2: First non-wireless adapter with valid IP
        var wiredAdapter = adapters
            .Where(a => !a.IsWireless)
            .FirstOrDefault();
            
        if (wiredAdapter != null)
            return wiredAdapter;

        // Priority 3: Any connected adapter
        return adapters.FirstOrDefault();
    }

    // Properties to control button visibility (with property change notifications)
    public bool CanAddNic => !IsNic4Visible;
    public bool CanRemoveNic => IsNic2Visible || IsNic3Visible || IsNic4Visible;
    public bool CanAddStorage => !IsDrive4Visible;
    public bool CanRemoveStorage => IsDrive2Visible || IsDrive3Visible || IsDrive4Visible;

    // Override the generated property setters to trigger button visibility updates
    partial void OnIsNic2VisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAddNic));
        OnPropertyChanged(nameof(CanRemoveNic));
    }

    partial void OnIsNic3VisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAddNic));
        OnPropertyChanged(nameof(CanRemoveNic));
    }

    partial void OnIsNic4VisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAddNic));
        OnPropertyChanged(nameof(CanRemoveNic));
    }

    partial void OnIsDrive2VisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAddStorage));
        OnPropertyChanged(nameof(CanRemoveStorage));
    }

    partial void OnIsDrive3VisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAddStorage));
        OnPropertyChanged(nameof(CanRemoveStorage));
    }

    [RelayCommand]
    private async Task UpdateDevice()
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(Hostname))
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Hostname is required.");
                return;
            }

            if (SelectedDevice == null)
            {
                await _dialogService.ShowAlertAsync("Update Error", "No device selected for update.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ChangeReason))
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Change reason is required for device updates.");
                return;
            }

            // Get audit information
            var applicationUser = GetApplicationUser();
            var discoverySessionId = _currentDiscoverySessionId.ToString();

            // Create device object from form fields
            var device = new Models.Device
            {
                device_id = SelectedDevice.device_id,
                Hostname = Hostname?.Trim() ?? string.Empty,
                SerialNumber = Serial_number?.Trim(),
                AssetTag = Asset_tag?.Trim(),
                DomainName = Domain_name?.Trim(),
                IsDomainJoined = Is_domain_joined,
                Manufacturer = Manufacturer?.Trim(),
                Model = Model?.Trim(),
                CpuInfo = Cpu_info?.Trim(),
                TotalRamGb = Total_ram_gb,
                RamType = Ram_type?.Trim(),
                RamSpeed = Ram_speed?.Trim(),
                RamManufacturer = Ram_manufacturer?.Trim(),
                StorageInfo = Storage_info?.Trim(),
                StorageType = Storage_type?.Trim(),
                StorageModel = Storage_model?.Trim(),
                BiosVersion = Bios_version?.Trim(),
                OsName = Os_name?.Trim(),
                OSVersion = Os_version?.Trim(),
                OsArchitecture = Os_architecture?.Trim(),
                PrimaryIp = PrimaryIp?.Trim(),
                PrimaryMac = PrimaryMac?.Trim(),
                PrimarySubnet = PrimarySubnet?.Trim(),
                PrimaryDns = PrimaryDns?.Trim(),
                SecondaryDns = SecondaryDns?.Trim(),
                
                // Network Interfaces 2-4
                Nic2Name = Nic2Name?.Trim(),
                Nic2Ip = Nic2Ip?.Trim(),
                Nic2Mac = Nic2Mac?.Trim(),
                Nic2Subnet = Nic2Subnet?.Trim(),
                Nic3Name = Nic3Name?.Trim(),
                Nic3Ip = Nic3Ip?.Trim(),
                Nic3Mac = Nic3Mac?.Trim(),
                Nic3Subnet = Nic3Subnet?.Trim(),
                Nic4Name = Nic4Name?.Trim(),
                Nic4Ip = Nic4Ip?.Trim(),
                Nic4Mac = Nic4Mac?.Trim(),
                Nic4Subnet = Nic4Subnet?.Trim(),
                
                // Storage Drives 2-4
                Drive2Name = Drive2Name?.Trim(),
                Drive2Capacity = Drive2Capacity?.Trim(),
                Drive2Type = Drive2Type?.Trim(),
                Drive2Model = Drive2Model?.Trim(),
                Drive3Name = Drive3Name?.Trim(),
                Drive3Capacity = Drive3Capacity?.Trim(),
                Drive3Type = Drive3Type?.Trim(),
                Drive3Model = Drive3Model?.Trim(),
                Drive4Name = Drive4Name?.Trim(),
                Drive4Capacity = Drive4Capacity?.Trim(),
                Drive4Type = Drive4Type?.Trim(),
                Drive4Model = Drive4Model?.Trim(),
                
                // Device Classification and Location
                device_type = Device_type?.Trim(),
                DeviceStatus = Device_status?.Trim(),
                EquipmentGroup = Equipment_group?.Trim(),
                Area = Area?.Trim(),
                Zone = Zone?.Trim(),
                Line = Line?.Trim(),
                Pitch = Pitch?.Trim(),
                Floor = Floor?.Trim(),
                Pillar = Pillar?.Trim(),
                
                // Additional Information
                AdditionalNotes = Additional_notes?.Trim(),
                WebInterfaceUrl = WebInterfaceUrl?.Trim(),
                DiscoveryMethod = Discovery_method?.Trim(),
                
                // System Fields
                UpdatedAt = DateTime.Now,
                LastDiscovered = Last_discovered
            };

            // Use the audit-enabled UpdateDeviceService
            var success = await _updateDeviceService.UpdateDeviceAsync(
                device,
                applicationUser,
                discoverySessionId,
                ChangeReason ?? "Device update",
                device.device_type ?? "Other");

            if (success)
            {
                StatusMessage = $"Device '{device.Hostname}' updated successfully!";
                IsStatusVisible = true;
                _logger.LogInformation("Updated device: {Hostname} with reason: {ChangeReason}", device.Hostname, ChangeReason);

                // Clear the form and exit edit mode after successful update
                ClearForm();
                IsEditMode = false;
                EditButtonText = "Edit";
                SelectedDevice = null;
                
                await _dialogService.ShowAlertAsync("Update Success", $"Device '{device.Hostname}' has been updated successfully.");
            }
            else
            {
                StatusMessage = $"Failed to update device '{device.Hostname}'. Please try again.";
                IsStatusVisible = true;
                _logger.LogError("Failed to update device: {Hostname}", device.Hostname);
                await _dialogService.ShowAlertAsync("Update Failed", $"Failed to update device '{device.Hostname}'. Please check the logs and try again.");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating device: {ex.Message}";
            IsStatusVisible = true;
            _logger.LogError(ex, "Error in UpdateDevice");
            await _dialogService.ShowAlertAsync("Update Error", $"An error occurred while updating the device: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BrowseBulkScanFile()
    {
        try
        {
            var fileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".txt" } },
                { DevicePlatform.macOS, new[] { "txt" } },
            });

            var options = new PickOptions
            {
                PickerTitle = "Select text file with hostnames/IP addresses",
                FileTypes = fileType,
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                BulkScanFilePath = result.FullPath;
                _logger.LogInformation("Selected bulk scan file: {FilePath}", BulkScanFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for bulk scan file");
            await _dialogService.ShowAlertAsync("File Selection Error", $"Error selecting file: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StartBulkScan()
    {
        _logger.LogInformation("StartBulkScan command executed");
        
        if (string.IsNullOrWhiteSpace(BulkScanFilePath))
        {
            await _dialogService.ShowAlertAsync("Validation Error", "Please select a file first.");
            return;
        }

        if (!File.Exists(BulkScanFilePath))
        {
            await _dialogService.ShowAlertAsync("File Error", "The selected file does not exist.");
            return;
        }

        if (string.IsNullOrWhiteSpace(BulkScanChangeReason))
        {
            await _dialogService.ShowAlertAsync("Validation Error", "Please provide a change reason.");
            return;
        }

        try
        {
            IsBulkScanning = true;
            BulkScanProgress = 0;
            BulkScanStatusMessage = "Starting bulk scan...";

            // Use the current user and create a new discovery session
            var applicationUser = "admin"; // TODO: Get from authentication service
            var discoverySessionId = Guid.NewGuid();

            _lastBulkScanResult = await _bulkDeviceScanService.ProcessBulkScanAsync(
                BulkScanFilePath,
                applicationUser,
                discoverySessionId,
                BulkScanChangeReason);

            var statusMessage = $"Bulk scan completed! Added: {_lastBulkScanResult.SuccessfullyAdded}, Failed: {_lastBulkScanResult.Failed}, Skipped: {_lastBulkScanResult.Skipped}";
            
            if (_lastBulkScanResult.Failed > 0 && !string.IsNullOrEmpty(_lastBulkScanResult.FailureReportPath))
            {
                statusMessage += $" - Failure report saved to file.";
            }
            
            BulkScanStatusMessage = statusMessage;
            HasBulkScanResults = true;
            
            _logger.LogInformation("Bulk scan completed. Success: {Success}, Failed: {Failed}, Skipped: {Skipped}", 
                _lastBulkScanResult.SuccessfullyAdded, _lastBulkScanResult.Failed, _lastBulkScanResult.Skipped);

            // Refresh the device list to show newly added devices
            await LoadAllDevices();
        }
        catch (Exception ex)
        {
            BulkScanStatusMessage = $"Bulk scan failed: {ex.Message}";
            _logger.LogError(ex, "Error during bulk scan");
            await _dialogService.ShowAlertAsync("Bulk Scan Error", $"Error during bulk scan: {ex.Message}");
        }
        finally
        {
            IsBulkScanning = false;
        }
    }

    [RelayCommand]
    private async Task ViewBulkScanResults()
    {
        if (_lastBulkScanResult == null)
        {
            await _dialogService.ShowAlertAsync("No Results", "No bulk scan results available.");
            return;
        }

        var resultSummary = $"Bulk Scan Results:\n\n" +
                           $"Total devices processed: {_lastBulkScanResult.TotalProcessed}\n" +
                           $"Successfully added: {_lastBulkScanResult.SuccessfullyAdded}\n" +
                           $"Failed to scan: {_lastBulkScanResult.Failed}\n" +
                           $"Skipped devices: {_lastBulkScanResult.Skipped}\n" +
                           $"Scan duration: {_lastBulkScanResult.Duration:hh\\:mm\\:ss}\n";

        // Add failure report information if available
        if (!string.IsNullOrEmpty(_lastBulkScanResult.FailureReportPath))
        {
            resultSummary += $"\nFailure report saved to:\n{_lastBulkScanResult.FailureReportPath}\n";
        }

        resultSummary += "\n";

        if (_lastBulkScanResult.DeviceResults.Any(d => d.Action == BulkScanDeviceAction.Failed))
        {
            resultSummary += "Failed devices:\n";
            foreach (var failed in _lastBulkScanResult.DeviceResults.Where(d => d.Action == BulkScanDeviceAction.Failed))
            {
                resultSummary += $"• {failed.HostnameOrIP}: {failed.ErrorMessage}\n";
            }
            
            if (!string.IsNullOrEmpty(_lastBulkScanResult.FailureReportPath))
            {
                resultSummary += $"\nA detailed failure report with retry instructions has been saved to your file location.";
            }
        }

        await _dialogService.ShowAlertAsync("Bulk Scan Results", resultSummary);
    }

    private void ClearEditForm()
    {
        // Clear all edit fields
        EditHostname = string.Empty;
        EditSerialNumber = string.Empty;
        EditAssetTag = string.Empty;
        EditDomainName = string.Empty;
        EditWorkgroup = string.Empty;
        EditIsDomainJoined = true;
        EditManufacturer = string.Empty;
        EditModel = string.Empty;
        EditCpuInfo = string.Empty;
        EditTotalRamGb = 0;
        EditRamType = string.Empty;
        EditRamSpeed = string.Empty;
        EditRamManufacturer = string.Empty;
        EditStorageInfo = string.Empty;
        EditStorageType = string.Empty;
        EditStorageModel = string.Empty;
        EditBiosVersion = string.Empty;
        EditOsName = string.Empty;
        EditOSVersion = string.Empty;
        EditOsArchitecture = string.Empty;
        EditPrimaryIp = string.Empty;
        EditPrimaryMac = string.Empty;
        EditPrimarySubnet = string.Empty;
        EditPrimaryDns = string.Empty;
        EditSecondaryDns = string.Empty;
        EditNic2Name = string.Empty;
        EditNic2Ip = string.Empty;
        EditNic2Mac = string.Empty;
        EditNic2Subnet = string.Empty;
        EditNic3Name = string.Empty;
        EditNic3Ip = string.Empty;
        EditNic3Mac = string.Empty;
        EditNic3Subnet = string.Empty;
        EditNic4Name = string.Empty;
        EditNic4Ip = string.Empty;
        EditNic4Mac = string.Empty;
        EditNic4Subnet = string.Empty;
        EditDeviceType = string.Empty;
        EditDeviceStatus = string.Empty;
        EditWebLink = string.Empty;
        EditArea = string.Empty;
        EditZone = string.Empty;
        EditLine = string.Empty;
        EditPitch = string.Empty;
        EditFloor = string.Empty;
        EditPillar = string.Empty;
        EditEquipmentGroup = string.Empty;
        EditAdditionalNotes = string.Empty;
        EditDiscoveryMethod = string.Empty;
        ChangeReason = string.Empty;
        SelectedDevice = null;
    }

    partial void OnIsDrive4VisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAddStorage));
        OnPropertyChanged(nameof(CanRemoveStorage));
    }

    // Bulk scan property change handlers
    partial void OnBulkScanFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(CanStartBulkScan));
    }

    partial void OnBulkScanChangeReasonChanged(string value)
    {
        OnPropertyChanged(nameof(CanStartBulkScan));
    }

    partial void OnIsBulkScanningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStartBulkScan));
    }

}

// Helper classes for collection binding
public partial class NetworkAdapterItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private string _macAddress = string.Empty;

    [ObservableProperty]
    private string _subnet = string.Empty;

    [ObservableProperty]
    private string _subnetMask = string.Empty;

    partial void OnSubnetChanged(string value) => SubnetMask = value;
    partial void OnSubnetMaskChanged(string value) => Subnet = value;
}

public partial class StorageDriveItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _capacity = string.Empty;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;
}
