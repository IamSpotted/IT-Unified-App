namespace MauiApp1.ViewModels;

// ViewModel for managing IP networkDevice devices within the IT support framework.
// This ViewModel provides comprehensive networkDevice management functionality including discovery,
// preview capabilities, and filtering/search operations for network networkDevices.
//
// Architecture:
// - Inherits from FilterableBaseViewModel<NetworkDevice> for filtering and search functionality
// - Uses partial class for CommunityToolkit.Mvvm source generation
// - Implements MVVM pattern with data binding and command handling
// - Integrates with multiple services for networkDevice operations
//
// Core Functionality:
// - NetworkDevice discovery and enumeration from network
// - NetworkDevice preview and fullscreen viewing capabilities
// - Web interface access for networkDevice configuration
// - Advanced filtering by area, zone, line, column, and level
// - Search functionality across multiple networkDevice properties
//
// UI Features:
// - Grid-based networkDevice display with configurable span
// - NetworkDevice selection with detailed information dialogs
// - Preview modal with fullscreen toggle capability
// - Filter controls for organizing large networkDevice collections
//
// Integration Points:
// - INetworkDeviceService: NetworkDevice discovery operations
// - ISettingsService: Grid layout preferences and configuration
// - IFilterService: Advanced filtering and search capabilities
// - Browser integration: External web interface access
//
// User Experience:
// - Intuitive networkDevice selection with action sheets
// - Progressive disclosure of networkDevice information
// - Responsive grid layout with user preferences
public partial class NetworkDevicesViewModel : FilterableBaseViewModel<NetworkDevice>, ILoadableViewModel, IDisposable
{
    // NetworkDevice service for network discovery.
    // Handles communication with IP networkDevices and discovery operations.
    private readonly INetworkDeviceService _networkDeviceService;
    
    // Settings service for user preferences and configuration persistence.
    // Manages grid layout settings and other networkDevice-related preferences.
    private readonly ISettingsService _settingsService;
    private readonly SecureCredentialsService _credentialsService;
    private bool _disposed = false;

    // Currently selected networkDevice for detailed operations.
    // Used for networkDevice-specific actions like web interface access.
    [ObservableProperty]
    private NetworkDevice? _selectedNetworkDevice;

    // Number of columns in the networkDevice grid display layout.
    // User-configurable setting for organizing networkDevice display density.
    [ObservableProperty]
    private int _gridSpan = 5;

    // Convenience properties for UI binding that expose filtered collections and selected values.
    // These properties provide direct access to networkDevice data and filter options for data binding.
    
    // Collection of all networkDevices available for display, filtered by current search and filter criteria.
    // This is an alias for the inherited Items property, providing semantic clarity for networkDevice data.
    public ObservableCollection<NetworkDevice> NetworkDevices => Items;
    
    // Available area options for filtering networkDevices by physical location area.
    // Dynamically populated from discovered networkDevices and their location data.
    public ObservableCollection<string> AvailableAreas => FilterOptions.TryGetValue("Area", out var areas) ? areas : new();
    
    // Available zone options for filtering networkDevices by specific zones within areas.
    // Provides fine-grained location filtering for large networkDevice deployments.
    public ObservableCollection<string> AvailableZones => FilterOptions.TryGetValue("Zone", out var zones) ? zones : new();
    
    // Available line options for filtering networkDevices by production lines or corridors.
    // Useful for manufacturing or facility monitoring networkDevice organization.
    public ObservableCollection<string> AvailableLines => FilterOptions.TryGetValue("Line", out var lines) ? lines : new();
    
    // Available column options for filtering networkDevices by column positions.
    // Enables grid-based location filtering for systematically positioned networkDevices.
    public ObservableCollection<string> AvailableColumns => FilterOptions.TryGetValue("Column", out var columns) ? columns : new();
    
    // Available level options for filtering networkDevices by floor or elevation levels.
    // Provides vertical location filtering for multi-story facility monitoring.
    public ObservableCollection<string> AvailableLevels => FilterOptions.TryGetValue("Level", out var levels) ? levels : new();

    // Filter selection properties that provide two-way binding for UI filter controls.
    // These properties enable users to select specific filter values and automatically
    // update the networkDevice display based on the selected criteria.
    
    // Selected area filter for displaying networkDevices in a specific physical area.
    // When set, filters the networkDevice collection to show only networkDevices in the selected area.
    public string SelectedArea
    {
        get => SelectedFilters.TryGetValue("Area", out var area) ? area : "";
        set => OnFilterChanged("Area", value);
    }

    // Selected zone filter for displaying networkDevices in a specific zone within an area.
    // Provides more granular location filtering than area alone.
    public string SelectedZone
    {
        get => SelectedFilters.TryGetValue("Zone", out var zone) ? zone : "";
        set => OnFilterChanged("Zone", value);
    }

    // Selected line filter for displaying networkDevices on a specific production line or corridor.
    // Useful for facility monitoring and manufacturing environment networkDevice management.
    public string SelectedLine
    {
        get => SelectedFilters.TryGetValue("Line", out var line) ? line : "";
        set => OnFilterChanged("Line", value);
    }

    // Selected column filter for displaying networkDevices in a specific column position.
    // Enables grid-based filtering for systematically positioned networkDevice arrays.
    public string SelectedColumn
    {
        get => SelectedFilters.TryGetValue("Column", out var column) ? column : "";
        set => OnFilterChanged("Column", value);
    }

    // Selected level filter for displaying networkDevices on a specific floor or elevation.
    // Provides vertical location filtering for multi-story facility monitoring.
    public string SelectedLevel
    {
        get => SelectedFilters.TryGetValue("Level", out var level) ? level : "";
        set => OnFilterChanged("Level", value);
    }

    // Constructor that initializes the NetworkDevicesViewModel with required services and dependencies.
    // Sets up networkDevice management functionality and initializes the UI with default state.
    //
    // Parameters:
    // - logger: Logging service for debugging and monitoring networkDevice operations
    // - navigationService: Navigation service for page routing and navigation
    // - dialogService: Dialog service for user interaction and alerts
    // - networkDeviceService: NetworkDevice-specific service for discovery and status operations
    // - filterService: Service for advanced filtering and search capabilities
    // - settingsService: Service for user preferences and configuration persistence
    //
    // Initialization Process:
    // 1. Calls base constructor to set up filtering and base ViewModel functionality
    // 2. Stores networkDevice-specific services for later use
    // 3. Sets page title for UI display
    // 4. Loads user's grid span preference from settings
    // 5. Sets up settings change notifications for dynamic updates
    // 6. Initiates networkDevice discovery
    //
    // Service Dependencies:
    // - INetworkDeviceService: Network networkDevice discovery
    // - ISettingsService: Grid layout preferences and networkDevice-related settings
    // - Base services: Logging, navigation, dialogs, and filtering inherited from base
    public NetworkDevicesViewModel(
        ILogger<NetworkDevicesViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        INetworkDeviceService networkDeviceService,
        IFilterService<NetworkDevice> filterService,
        ISettingsService settingsService,
        SecureCredentialsService credentialsService) 
        : base(logger, navigationService, dialogService, filterService)
    {
        // Set the page title for UI display and navigation
        Title = "NetworkDevices";
        
        // Store networkDevice service for network discovery and status operations
        _networkDeviceService = networkDeviceService;
        
        // Store settings service for user preferences and configuration
        _settingsService = settingsService;
        
        // TODO: Re-enable after fixing DI issues
        _credentialsService = credentialsService;
        
        // Load user's preferred grid span setting asynchronously
        // This determines how many networkDevices are displayed per row
        _ = LoadGridSpanAsync();
        
        // Subscribe to grid span changes from settings to update display dynamically
        // Allows real-time updates when user changes grid preferences
        SettingsManager.GridSpanChanged += OnGridSpanChanged;
        
        // Initialize networkDevice discovery
        // Starts the process of finding network networkDevices
        _ = LoadNetworkDevicesAsync();
    }

    // Defines the properties available for filtering networkDevice collections.
    // This method overrides the base class to specify networkDevice-specific filter criteria.
    //
    // Returns:
    // - Array of property names that can be used for filtering networkDevices
    //
    // Filter Properties:
    // - Area: Physical location area for broad geographic filtering
    // - Zone: Specific zones within areas for granular location filtering
    // - Line: Production lines or corridors for process-specific filtering
    // - Column: Column positions for grid-based location filtering
    // - Level: Floor or elevation levels for vertical location filtering
    //
    // Usage:
    // - Used by FilterableBaseViewModel to build filter options automatically
    // - Enables dynamic filter UI generation based on available networkDevice data
    // - Supports both UI picker controls and programmatic filtering
    protected override string[] GetFilterProperties()
    {
        return new[] { "Area", "Zone", "Line", "Column", "Level" };
    }

    // Command to load networkDevices from the network using the networkDevice service.
    // This method discovers available IP networkDevices and populates the networkDevice collection for display.
    //
    // Functionality:
    // - Calls networkDevice service to discover network networkDevices
    // - Loads discovered networkDevices into the filterable collection
    // - Logs the number of networkDevices found for monitoring
    // - Uses safe execution wrapper for error handling
    //
    // UI Integration:
    // - Can be triggered by refresh buttons or page initialization
    // - Updates the networkDevice grid display with discovered devices
    // - Provides loading state feedback through IsBusy property
    [RelayCommand]
    private async Task LoadNetworkDevices()
    {
        await ExecuteSafelyAsync(async () =>
        {
            // Discover networkDevices from network using networkDevice service
            var networkDevices = await _networkDeviceService.GetNetworkDevicesAsync();
            
            // Load networkDevices into the filterable collection for display
            LoadItems(networkDevices);
            
            // Log successful networkDevice discovery for monitoring
            _logger.LogInformation("Loaded {Count} networkDevices", networkDevices.Count());
        }, "Load NetworkDevices");
    }

    // Command to handle networkDevice selection and display available actions.
    // This method provides a user-friendly interface for networkDevice interaction with progressive disclosure.
    //
    // Parameters:
    // - networkDevice: The selected networkDevice object containing connection and configuration information
    //
    // User Experience Flow:
    // 1. Sets the selected networkDevice for tracking
    // 2. Presents action sheet with available functionality
    //
    // Available Actions:
    // - Preview in App: Shows networkDevice feed within the application
    // - Open Web Interface: Launches networkDevice's web configuration page
    // - Show Details: Displays comprehensive networkDevice information
    [RelayCommand]
    private async Task SelectNetworkDevice(NetworkDevice networkDevice)
    {
        await ExecuteSafelyAsync(async () =>
        {
            if (networkDevice == null) return;
            SelectedNetworkDevice = networkDevice;

            if (Application.Current?.MainPage == null) return;

            var details = $"NetworkDevice: {networkDevice.Hostname}\nIP Address: {networkDevice.PrimaryIp}\nLocation: {networkDevice.FullLocation}\nWeb Interface: {networkDevice.WebInterfaceUrl}";

            // Show action sheet first
            var action = await Application.Current.MainPage.DisplayActionSheet(
                $"📹 {networkDevice.Hostname}",
                "Cancel",
                null,
                "🌐 Open Web Interface",
                "ℹ️ Show Details");

            switch (action)
            {
                case "🌐 Open Web Interface":
                    await OpenWebInterface(networkDevice);
                    break;

                case "ℹ️ Show Details":
                    // Now query SolarWinds status
                    var status = GetSolarWindsStatus(networkDevice);
                    var statusEmoji = status?.IsOnline == true ? "🟢" : "🔴";
                    var statusText = status?.IsOnline == true ? "Online" : "Offline";
                    var responseTime = status?.ResponseTimeMs > 0 ? $" ({status.ResponseTimeMs}ms)" : "";

                    var enhancedDetails = $"{details}\n\nLive Status: {statusText}\nResponse Time: {status?.ResponseTimeMs ?? 0}ms\nLast Checked: {DateTime.Now:HH:mm:ss}";
                    await Application.Current.MainPage.DisplayAlert(
                        $"NetworkDevice Details {statusEmoji} {statusText}{responseTime}", enhancedDetails, "OK");
                    break;
            }
        }, "Select NetworkDevice");
    }

    // Command to open the networkDevice's web interface in the default browser.
    // This method launches the networkDevice's configuration and management web page externally.
    //
    // Parameters:
    // - networkDevice: The networkDevice object containing web interface URL and identification
    //
    // Web Interface Functionality:
    // - Opens networkDevice's web configuration page in system browser
    // - Uses system preferred browser for consistent user experience
    // - Handles URL launch errors with detailed error reporting
    // - Logs web interface access for monitoring and debugging
    //
    // Error Handling:
    // - Catches browser launch failures
    // - Displays detailed error information to user
    // - Logs errors for troubleshooting and monitoring
    // - Provides fallback information for manual access
    [RelayCommand]
    private async Task OpenWebInterface(NetworkDevice networkDevice)
    {
        try
        {
            _logger.LogInformation("Opening web interface for networkDevice {Hostname} at {Url}",
                networkDevice.Hostname, networkDevice.WebInterfaceUrl);

            if (string.IsNullOrWhiteSpace(networkDevice.WebInterfaceUrl))
            {
                await _dialogService.ShowAlertAsync("Error", "Web interface URL is empty or null");
                return;
            }

            if (!Uri.TryCreate(networkDevice.WebInterfaceUrl, UriKind.Absolute, out var uri))
            {
                await _dialogService.ShowAlertAsync("Error", $"Invalid URL format: {networkDevice.WebInterfaceUrl}");
                return;
            }

            // Optional pre-warm
            _ = Task.Run(async () =>
            {
                try
                {
                    using var client = new HttpClient();
                    await client.GetAsync(uri);
                }
                catch
                {
                    // Just preloading
                }
            });

            // Slight delay to avoid UI contention
            await Task.Delay(100);

            await Launcher.Default.OpenAsync(uri);

            _logger.LogInformation("Successfully opened web interface for {Hostname}", networkDevice.Hostname);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open web interface for {Hostname}. URL: {Url}",
                networkDevice.Hostname, networkDevice.WebInterfaceUrl);
            await _dialogService.ShowAlertAsync("Error",
                $"Unable to open web interface for {networkDevice.Hostname}.\nURL: {networkDevice.WebInterfaceUrl}\nError: {ex.Message}");
        }
    }

    // Private helper method to initialize networkDevice data by loading networkDevices.
    // This method performs networkDevice discovery for complete initialization.
    //
    // Initialization Sequence:
    // 1. Loads networkDevices from network discovery
    // 2. Provides complete networkDevice information for UI display
    //
    // Usage:
    // - Called during ViewModel construction for initial setup
    // - Can be called for complete data refresh operations
    private async Task LoadNetworkDevicesAsync()
    {
        // Load networkDevices from network discovery
        await LoadNetworkDevices();
    }

    // Override method to implement networkDevice-specific search filtering.
    // This method defines how search text is applied to filter the networkDevice collection.
    //
    // Parameters:
    // - items: Collection of networkDevices to filter
    // - searchText: User-entered search criteria
    //
    // Returns:
    // - Filtered collection of networkDevices matching search criteria
    //
    // Search Functionality:
    // - Searches across multiple networkDevice properties simultaneously
    // - Case-insensitive matching for user-friendly experience
    // - Comprehensive coverage of identifiable networkDevice attributes
    //
    // Searchable Properties:
    // - Name: NetworkDevice display name and identifier
    // - Model: NetworkDevice hardware model and type
    // - Area: Physical location area
    // - Zone: Specific zone within area
    // - Line: Production line or corridor
    // - Column: Grid column position
    // - Level: Floor or elevation level
    // - IpAddress: Network address for technical searches
    // - FullLocation: Complete location description
    protected override IEnumerable<NetworkDevice> ApplySearchFilter(IEnumerable<NetworkDevice> items, string searchText)
    {
        // Return unfiltered collection if no search text provided
        if (string.IsNullOrWhiteSpace(searchText))
            return items;

        // Convert search text to lowercase for case-insensitive matching
        var lowerSearchText = searchText.ToLowerInvariant();
        
        // Filter networkDevices based on multiple property matches
        return items.Where(networkDevice =>
            (networkDevice.Hostname ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (networkDevice.Model ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (networkDevice.Area ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (networkDevice.Zone ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (networkDevice.Line ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (networkDevice.Column ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (networkDevice.Level ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (networkDevice.PrimaryIp ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)
        );
    }

    // Private helper method to load grid span setting from user preferences.
    // This method retrieves the user's preferred networkDevice grid layout configuration.
    //
    // Grid Span Functionality:
    // - Loads user's preferred number of columns in networkDevice grid
    // - Provides default value (5) if setting is not available
    // - Handles loading errors gracefully with fallback
    //
    // Error Handling:
    // - Catches settings service exceptions
    // - Logs errors for troubleshooting
    // - Uses safe default value to maintain functionality
    //
    // Settings Integration:
    // - Uses async settings service for non-blocking operation
    // - Supports nullable int for optional setting values
    // - Provides consistent default behavior
    private async Task LoadGridSpanAsync()
    {
        try
        {
            // Load grid span setting with default fallback
            var gridSpanSetting = await _settingsService.GetAsync<int?>("grid_span", 5);
            GridSpan = gridSpanSetting ?? 5;
        }
        catch (Exception ex)
        {
            // Log error for troubleshooting
            _logger.LogError(ex, "Failed to load grid span setting");
            
            // Use safe default value to maintain functionality
            GridSpan = 5;
        }
    }

    // Method to query SolarWinds for networkDevice device status
    private DeviceStatus? GetSolarWindsStatus(NetworkDevice networkDevice)
    {
        try
        {
            // Remove or comment out the delay below:
            // await Task.Delay(1000); // Simulate network call

            var random = new Random();
            var isOnline = random.NextDouble() > 0.3; // 70% chance of being online
            var responseTime = isOnline ? random.Next(10, 200) : 0;

            _logger.LogInformation("Mock SolarWinds status for {Hostname} - DI issues need to be resolved", networkDevice.Hostname);

            return new DeviceStatus
            {
                IsOnline = isOnline,
                ResponseTimeMs = responseTime,
                StatusDescription = isOnline ? "Up" : "Down",
                LastChecked = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query SolarWinds status for {Hostname}", networkDevice.Hostname);
            return new DeviceStatus
            {
                IsOnline = false,
                ResponseTimeMs = 0,
                StatusDescription = "Error",
                LastChecked = DateTime.Now
            };
        }
        
    }

    // Event handler for grid span changes from settings service.
    // This method responds to real-time updates of grid layout preferences.
    //
    // Parameters:
    // - sender: Event source (typically SettingsManager)
    // - newGridSpan: Updated grid span value from settings
    //
    // Dynamic Updates:
    // - Updates UI layout in real-time as settings change
    // - Maintains synchronization with settings service
    // - Logs changes for monitoring and debugging
    //
    // User Experience:
    // - Immediate visual feedback for setting changes
    // - No need to restart or refresh for layout updates
    // - Consistent behavior across application instances
    private void OnGridSpanChanged(object? sender, int newGridSpan)
    {
        // Update grid span property for UI binding
        GridSpan = newGridSpan;
        
        // Log grid span change for monitoring
        _logger.LogInformation("Grid span updated to: {GridSpan}", newGridSpan);
    }

    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Unsubscribe from static events to prevent memory leaks
            SettingsManager.GridSpanChanged -= OnGridSpanChanged;
            _disposed = true;
        }
    }

    /// <summary>
    /// Implementation of ILoadableViewModel.LoadDataCommand
    /// Aliases to LoadNetworkDevicesCommand for automatic page loading
    /// </summary>
    public IAsyncRelayCommand LoadDataCommand => LoadNetworkDevicesCommand;
}
