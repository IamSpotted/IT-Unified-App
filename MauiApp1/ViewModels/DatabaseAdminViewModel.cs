using MauiApp1.Models;
using MauiApp1.Interfaces;
using MauiApp1.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using DeviceModel = MauiApp1.Models.Device;
using System.Text.Json;

namespace MauiApp1.ViewModels;

public partial class DatabaseAdminViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IAddDeviceService _addDeviceService;
    private readonly IUpdateDeviceService _updateDeviceService;
    private new readonly IDialogService _dialogService;
    private readonly IRemoveDeviceService? _removeDeviceService;

    public DatabaseAdminViewModel(
        ILogger<DatabaseAdminViewModel> logger,
        IDatabaseService databaseService,
        IAddDeviceService addDeviceService,
        IUpdateDeviceService updateDeviceService,
        INavigationService navigationService,
        IDialogService dialogService,
        IRemoveDeviceService removeDeviceService)
        : base(logger, navigationService, dialogService)
    {
        _databaseService = databaseService;
        _addDeviceService = addDeviceService;
        _updateDeviceService = updateDeviceService;
        _dialogService = dialogService;
        _removeDeviceService = removeDeviceService;
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
    private string _storage_info = string.Empty;

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
    private string _default_gateways = string.Empty;

    [ObservableProperty]
    private string _subnet_masks = string.Empty;

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
    private DeviceModel? _selectedDevice = null;

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
    private string _editSecondaryIps = string.Empty;
    [ObservableProperty]
    private string _editSecondaryMacs = string.Empty;
    [ObservableProperty]
    private string _editDnsServers = string.Empty;
    [ObservableProperty]
    private string _editDefaultGateways = string.Empty;
    [ObservableProperty]
    private string _editSubnetMasks = string.Empty;
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

    // Available options for dropdowns
    public List<string> device_type_options { get; } = InputSanitizer.DeviceTypeOptions.ToList();
    public List<string> device_status_options { get; } = InputSanitizer.DeviceStatusOptions.ToList();

    // Search Properties
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching = false;

    [ObservableProperty]
    private ObservableCollection<DeviceModel> _searchResults = new();

    [ObservableProperty]
    private bool _hasSearchResults = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isStatusVisible = false;

    // This is what controls the search of dbo.devices

    [RelayCommand]
    private async Task SearchDevices()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await _dialogService.ShowAlertAsync("Search Error", "Please enter a search term.");
            return;
        }

        IsSearching = true;
        SearchResults.Clear();
        HasSearchResults = false;

        try
        {
            var results = await _databaseService.SearchDevicesAsync(SearchQuery);

            foreach (var device in results)
            {
                SearchResults.Add(device);
            }

            HasSearchResults = SearchResults.Count > 0;

            if (!HasSearchResults)
            {
                await _dialogService.ShowAlertAsync("Search Results", $"No devices found matching '{SearchQuery}'.");
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
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        HasSearchResults = false;
    }

    [RelayCommand]
    private async Task ViewDevice(DeviceModel device)
    {
        if (device == null) return;

        var devices = $"Device: {device.Hostname}\n" +
                        $"IP Address: {device.IpAddress ?? "N/A"}\n" +
                        $"Location: Area: {device.Area} Zone: {device.Zone} Line: {device.Line} Pitch: {device.Pitch}\n" +
                        $"Column: {device.Pillar}\n" +
                        $"Level: {device.Floor}\n" +
                        $"Type: {device.device_type}\n" +
                        $"Serial Number: {device.SerialNumber ?? "N/A"}\n" +
                        $"Asset Tag: {device.AssetTag ?? "N/A"}\n";

        await _dialogService.ShowAlertAsync($"Device Information - {device.Hostname}", devices);
    }

    [RelayCommand]
    private async Task EditSearchedDevice(DeviceModel device)
    {
        if (device == null) return;

        // Clear search results and switch to edit mode
        SearchResults.Clear();
        HasSearchResults = false;
        SearchQuery = string.Empty;

        // Set the device for editing based on its type
        SelectedDevice = device;

        EditHostname = device.Hostname;
        EditSerialNumber = device.SerialNumber = string.Empty;
        EditAssetTag = device.AssetTag = string.Empty;
        EditDomainName = device.DomainName = string.Empty;
        EditWorkgroup = device.Workgroup = string.Empty;
        EditIsDomainJoined = device.IsDomainJoined ?? true;
        EditManufacturer = device.Manufacturer = string.Empty;
        EditModel = device.Model = string.Empty;
        EditCpuInfo = device.CpuInfo = string.Empty;
        EditTotalRamGb = (int)device.TotalRamGb;
        EditRamType = device.RamType = string.Empty;
        EditStorageInfo = device.StorageInfo = string.Empty;
        EditBiosVersion = device.BiosVersion = string.Empty;
        EditOsName = device.OsName = string.Empty;
        EditOSVersion = device.OSVersion = string.Empty;
        EditOsArchitecture = device.OsArchitecture = string.Empty;
        EditPrimaryIp = device.PrimaryIp = string.Empty;
        EditPrimaryMac = device.PrimaryMac = string.Empty;
        EditSecondaryIps = JsonSerializer.Serialize(device.SecondaryIps);
        EditSecondaryMacs = JsonSerializer.Serialize(device.SecondaryMacs);
        EditDnsServers = JsonSerializer.Serialize(device.DnsServers);
        EditDefaultGateways = JsonSerializer.Serialize(device.DefaultGateways);
        EditSubnetMasks = JsonSerializer.Serialize(device.SubnetMasks);
        EditDeviceStatus = device.DeviceStatus = string.Empty;
        EditArea = device.Area = string.Empty;
        EditZone = device.Zone = string.Empty;
        EditLine = device.Line = string.Empty;
        EditPitch = device.Pitch = string.Empty;
        EditFloor = device.Floor = string.Empty;
        EditPillar = device.Pillar = string.Empty;
        EditAdditionalNotes = device.AdditionalNotes = string.Empty;
        EditUpdatedAt = device.UpdatedAt ?? DateTime.Now;
        EditLastDiscovered = device.LastDiscovered ?? DateTime.Now;
        EditDiscoveryMethod = device.DiscoveryMethod = string.Empty;
        EditDeviceType = device.device_type = string.Empty;
        EditWebLink = device.WebInterfaceUrl = string.Empty;
        EditEquipmentGroup = device.EquipmentGroup = string.Empty;

        // Switch to edit mode
        IsEditMode = true;
        EditButtonText = "Cancel";

        await _dialogService.ShowAlertAsync("Edit Mode", $"Now editing '{device.Hostname}'. Make your changes and click Save Changes.");
    }

    public DatabaseAdminViewModel(
        ILogger<DatabaseAdminViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        IDatabaseService databaseService,
        IAddDeviceService addDeviceService,
        IUpdateDeviceService updateDeviceService)
        : base(logger, navigationService, dialogService)
    {
        Title = "Database Admin";
        _dialogService = dialogService;
        _databaseService = databaseService;
        _addDeviceService = addDeviceService;
        _updateDeviceService = updateDeviceService;


        // Load existing devices
        _ = LoadAllDevicesCommand.ExecuteAsync(null);
    }

    [ObservableProperty]
    private ObservableCollection<DeviceModel> _devices = new();
    [RelayCommand]
    private async Task LoadAllDevices()
    {
        await ExecuteSafelyAsync(async () =>
        {
            try
            {
                var devices = await _databaseService.GetDevicesAsync();
                Devices = new ObservableCollection<DeviceModel>(devices);

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

                var device = new DeviceModel
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
                    SecondaryIps = JsonSerializer.Serialize(Secondary_ips.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                    SecondaryMacs = JsonSerializer.Serialize(Secondary_macs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                    DnsServers = JsonSerializer.Serialize(Dns_servers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                    DefaultGateways = JsonSerializer.Serialize(Default_gateways.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                    SubnetMasks = JsonSerializer.Serialize(Subnet_masks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
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
                    WebLink = WebInterfaceUrl,
                    EquipmentGroup = Equipment_group
                };

                var success = await _databaseService.AddDeviceAsync(device, SelectedDeviceType);

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
    private async Task DeleteDevice(DeviceModel device)
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

        var success = await _removeDeviceService.RemoveDeviceAsync(device.Hostname);
        if (success)
        {
            await _dialogService.ShowAlertAsync("Delete Success", $"{device.Hostname} has been deleted.");
            Devices.Remove(device);
            SelectedDevice = null;
        }
        else
        {
            await _dialogService.ShowAlertAsync("Delete Error", "Failed to delete device.");
        }
    }

    private void ClearForm()
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
        Storage_info = string.Empty;
        Bios_version = string.Empty;
        Os_name = string.Empty;
        Os_version = string.Empty;
        Os_architecture = string.Empty;
        PrimaryIp = string.Empty;
        PrimaryMac = string.Empty;
        Secondary_ips = string.Empty;
        Secondary_macs = string.Empty;
        Dns_servers = string.Empty;
        Default_gateways = string.Empty;
        Subnet_masks = string.Empty;
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
        SelectedDevice = null;
        IsEditMode = false;
        EditButtonText = "Edit";
        SearchQuery = string.Empty;
        HasSearchResults = false;
        SearchResults.Clear();
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

}