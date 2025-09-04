using MauiApp1.Interfaces;
using MauiApp1.Models;
using MauiApp1.ViewModels;

namespace MauiApp1.Services
{
    /// <summary>
    /// Service for handling device editing operations including form population and state management
    /// </summary>
    public class EditDeviceService : IEditDeviceService
    {
        private readonly ILogger<EditDeviceService> _logger;
        private readonly IDialogService _dialogService;

        public EditDeviceService(ILogger<EditDeviceService> logger, IDialogService dialogService)
        {
            _logger = logger;
            _dialogService = dialogService;
        }

        /// <summary>
        /// Populates the form fields with device data and sets edit mode
        /// </summary>
        public async Task PopulateFormForEditAsync(Models.Device device, object viewModel)
        {
            if (device == null)
            {
                _logger.LogWarning("PopulateFormForEditAsync called with null device");
                await _dialogService.ShowAlertAsync("Error", "No device selected for editing.");
                return;
            }

            if (viewModel is not DatabaseAdminViewModel vm)
            {
                _logger.LogError("PopulateFormForEditAsync called with invalid viewModel type: {Type}", viewModel?.GetType()?.Name ?? "null");
                await _dialogService.ShowAlertAsync("Error", "Invalid view model for editing operation.");
                return;
            }

            try
            {
                _logger.LogInformation("Populating form for editing device: {Hostname} (ID: {DeviceId})", device.Hostname, device.device_id);

                // Populate all form fields using the SAME property names as the scan functionality
                // Basic Information
                vm.Hostname = device.Hostname?.Trim() ?? string.Empty;
                _logger.LogDebug("Set Hostname: '{Hostname}'", vm.Hostname);
                
                vm.Serial_number = device.SerialNumber?.Trim() ?? string.Empty;
                _logger.LogDebug("Set Serial_number: '{SerialNumber}'", vm.Serial_number);
                
                vm.Asset_tag = device.AssetTag?.Trim() ?? string.Empty;
                vm.Domain_name = device.DomainName?.Trim() ?? string.Empty;
                vm.Is_domain_joined = device.IsDomainJoined ?? false;
                vm.Manufacturer = device.Manufacturer?.Trim() ?? string.Empty;
                vm.Model = device.Model?.Trim() ?? string.Empty;
                
                // Hardware Information
                vm.Cpu_info = device.CpuInfo?.Trim() ?? string.Empty;
                vm.Total_ram_gb = (int)device.TotalRamGb;
                vm.Ram_type = device.RamType?.Trim() ?? string.Empty;
                vm.Ram_speed = device.RamSpeed?.Trim() ?? string.Empty;
                vm.Ram_manufacturer = device.RamManufacturer?.Trim() ?? string.Empty;
                vm.Bios_version = device.BiosVersion?.Trim() ?? string.Empty;
                
                // Operating System Information
                vm.Os_name = device.OsName?.Trim() ?? string.Empty;
                vm.Os_version = device.OSVersion?.Trim() ?? string.Empty;
                vm.Os_architecture = device.OsArchitecture?.Trim() ?? string.Empty;
                
                // Storage Information
                vm.Storage_info = device.StorageInfo?.Trim() ?? string.Empty;
                vm.Storage_type = device.StorageType?.Trim() ?? string.Empty;
                vm.Storage_model = device.StorageModel?.Trim() ?? string.Empty;
                
                // Primary Network Interface (set both variants that scan uses)
                vm.PrimaryIp = device.PrimaryIp?.Trim() ?? string.Empty;
                vm.PrimaryMac = device.PrimaryMac?.Trim() ?? string.Empty;
                vm.PrimarySubnet = device.PrimarySubnet?.Trim() ?? string.Empty;
                vm.Subnet_masks = device.PrimarySubnet?.Trim() ?? string.Empty; // Also set this property that scan uses
                
                // DNS Information (set both variants that scan uses)
                vm.PrimaryDns = device.PrimaryDns?.Trim() ?? string.Empty;
                vm.Primary_dns = device.PrimaryDns?.Trim() ?? string.Empty;
                vm.SecondaryDns = device.SecondaryDns?.Trim() ?? string.Empty;
                vm.Secondary_dns = device.SecondaryDns?.Trim() ?? string.Empty;
                vm.Dns_servers = device.PrimaryDns?.Trim() ?? string.Empty; // Combined DNS field that scan uses
                
                // Additional Network Interfaces
                vm.Nic2Name = device.Nic2Name?.Trim() ?? string.Empty;
                vm.Nic2Ip = device.Nic2Ip?.Trim() ?? string.Empty;
                vm.Nic2Mac = device.Nic2Mac?.Trim() ?? string.Empty;
                vm.Nic2Subnet = device.Nic2Subnet?.Trim() ?? string.Empty;
                vm.IsNic2Visible = !string.IsNullOrEmpty(device.Nic2Name) || !string.IsNullOrEmpty(device.Nic2Ip);
                
                vm.Nic3Name = device.Nic3Name?.Trim() ?? string.Empty;
                vm.Nic3Ip = device.Nic3Ip?.Trim() ?? string.Empty;
                vm.Nic3Mac = device.Nic3Mac?.Trim() ?? string.Empty;
                vm.Nic3Subnet = device.Nic3Subnet?.Trim() ?? string.Empty;
                vm.IsNic3Visible = !string.IsNullOrEmpty(device.Nic3Name) || !string.IsNullOrEmpty(device.Nic3Ip);
                
                vm.Nic4Name = device.Nic4Name?.Trim() ?? string.Empty;
                vm.Nic4Ip = device.Nic4Ip?.Trim() ?? string.Empty;
                vm.Nic4Mac = device.Nic4Mac?.Trim() ?? string.Empty;
                vm.Nic4Subnet = device.Nic4Subnet?.Trim() ?? string.Empty;
                vm.IsNic4Visible = !string.IsNullOrEmpty(device.Nic4Name) || !string.IsNullOrEmpty(device.Nic4Ip);
                
                // Additional Storage Drives
                vm.Drive2Name = device.Drive2Name?.Trim() ?? string.Empty;
                vm.Drive2Capacity = device.Drive2Capacity?.Trim() ?? string.Empty;
                vm.Drive2Type = device.Drive2Type?.Trim() ?? string.Empty;
                vm.Drive2Model = device.Drive2Model?.Trim() ?? string.Empty;
                vm.IsDrive2Visible = !string.IsNullOrEmpty(device.Drive2Name) || !string.IsNullOrEmpty(device.Drive2Capacity);
                
                vm.Drive3Name = device.Drive3Name?.Trim() ?? string.Empty;
                vm.Drive3Capacity = device.Drive3Capacity?.Trim() ?? string.Empty;
                vm.Drive3Type = device.Drive3Type?.Trim() ?? string.Empty;
                vm.Drive3Model = device.Drive3Model?.Trim() ?? string.Empty;
                vm.IsDrive3Visible = !string.IsNullOrEmpty(device.Drive3Name) || !string.IsNullOrEmpty(device.Drive3Capacity);
                
                vm.Drive4Name = device.Drive4Name?.Trim() ?? string.Empty;
                vm.Drive4Capacity = device.Drive4Capacity?.Trim() ?? string.Empty;
                vm.Drive4Type = device.Drive4Type?.Trim() ?? string.Empty;
                vm.Drive4Model = device.Drive4Model?.Trim() ?? string.Empty;
                vm.IsDrive4Visible = !string.IsNullOrEmpty(device.Drive4Name) || !string.IsNullOrEmpty(device.Drive4Capacity);
                
                // Device Classification and Location
                vm.Device_type = device.device_type?.Trim() ?? device.DeviceType?.Trim() ?? string.Empty;
                vm.Device_status = device.DeviceStatus?.Trim() ?? device.device_status?.Trim() ?? string.Empty;
                vm.Equipment_group = device.EquipmentGroup?.Trim() ?? string.Empty;
                
                // Location Information
                vm.Area = device.Area?.Trim() ?? string.Empty;
                vm.Zone = device.Zone?.Trim() ?? string.Empty;
                vm.Line = device.Line?.Trim() ?? string.Empty;
                vm.Pitch = device.Pitch?.Trim() ?? string.Empty;
                vm.Floor = device.Floor?.Trim() ?? string.Empty;
                vm.Pillar = device.Pillar?.Trim() ?? string.Empty;
                
                // Additional Information
                vm.Additional_notes = device.AdditionalNotes?.Trim() ?? string.Empty;
                vm.WebInterfaceUrl = device.WebInterfaceUrl?.Trim() ?? string.Empty;
                vm.Discovery_method = device.DiscoveryMethod?.Trim() ?? string.Empty;
                
                // System Fields
                vm.Updated_at = device.UpdatedAt ?? DateTime.Now;
                vm.Last_discovered = device.LastDiscovered ?? DateTime.Now;

                // Set edit mode and store selected device
                await SetEditModeAsync(vm, true, device);
                
                // Force UI refresh for key fields to ensure they display properly
                vm.OnPropertyChanged(nameof(vm.Hostname));
                vm.OnPropertyChanged(nameof(vm.Serial_number));
                vm.OnPropertyChanged(nameof(vm.Asset_tag));
                vm.OnPropertyChanged(nameof(vm.Manufacturer));
                vm.OnPropertyChanged(nameof(vm.Model));
                vm.OnPropertyChanged(nameof(vm.PrimaryIp));
                vm.OnPropertyChanged(nameof(vm.PrimaryMac));
                vm.OnPropertyChanged(nameof(vm.Device_type));
                vm.OnPropertyChanged(nameof(vm.Device_status));
                vm.OnPropertyChanged(nameof(vm.Area));
                vm.OnPropertyChanged(nameof(vm.Zone));
                
                // Status and dialog are handled in SetEditModeAsync and below
                vm.StatusMessage = $"Device '{device.Hostname}' loaded for editing. Make your changes and click 'Save Changes'.";
                vm.IsStatusVisible = true;

                await _dialogService.ShowAlertAsync("Edit Mode", $"Device '{device.Hostname}' has been loaded into the form for editing. Make your changes and click 'Save Changes' when done.");
                
                _logger.LogInformation("Successfully populated form for editing device: {Hostname}", device.Hostname);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating form for editing device: {Hostname}", device.Hostname);
                await _dialogService.ShowAlertAsync("Edit Error", $"Unable to load device for editing: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the edit form and resets edit mode
        /// </summary>
        public async Task ClearEditFormAsync(object viewModel)
        {
            if (viewModel is not DatabaseAdminViewModel vm)
            {
                _logger.LogError("ClearEditFormAsync called with invalid viewModel type: {Type}", viewModel?.GetType()?.Name ?? "null");
                return;
            }

            try
            {
                _logger.LogInformation("Clearing edit form and resetting edit mode");
                
                // Use the existing ClearForm method in the ViewModel
                vm.ClearForm();
                
                // Ensure edit mode is disabled
                await SetEditModeAsync(vm, false, null);
                
                // Show confirmation message
                vm.StatusMessage = "Edit mode cancelled. Form has been cleared.";
                vm.IsStatusVisible = true;
                
                _logger.LogInformation("Successfully cleared edit form");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing edit form");
                await _dialogService.ShowAlertAsync("Error", $"Error clearing form: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the edit mode state and UI indicators
        /// </summary>
        public async Task SetEditModeAsync(object viewModel, bool isEditMode, Models.Device? selectedDevice = null)
        {
            if (viewModel is not DatabaseAdminViewModel vm)
            {
                _logger.LogError("SetEditModeAsync called with invalid viewModel type: {Type}", viewModel?.GetType()?.Name ?? "null");
                return;
            }

            try
            {
                _logger.LogInformation("Setting edit mode: {IsEditMode} for device: {DeviceName}", 
                    isEditMode, selectedDevice?.Hostname ?? "None");
                
                vm.IsEditMode = isEditMode;
                vm.EditButtonText = isEditMode ? "Update" : "Edit";
                vm.SelectedDevice = selectedDevice;
                
                // Set change reason requirements for edit mode
                vm.SetChangeReasonForMode(isEditMode);
                
                _logger.LogInformation("Successfully set edit mode: {IsEditMode}", isEditMode);
                
                await Task.CompletedTask; // Make method async for future extensibility
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting edit mode: {IsEditMode}", isEditMode);
                await _dialogService.ShowAlertAsync("Error", $"Error setting edit mode: {ex.Message}");
            }
        }
    }
}
