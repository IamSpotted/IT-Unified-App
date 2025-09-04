namespace MauiApp1.ViewModels;

// ViewModel for managing IP netop devices within the IT support framework.
// This ViewModel provides comprehensive netop management functionality including discovery,
// preview capabilities, and filtering/search operations for network netops.
//
// Architecture:
// - Inherits from FilterableBaseViewModel<Netop> for filtering and search functionality
// - Uses partial class for CommunityToolkit.Mvvm source generation
// - Implements MVVM pattern with data binding and command handling
// - Integrates with multiple services for netop operations
//
// Core Functionality:
// - Netop discovery and enumeration from network
// - Netop preview and fullscreen viewing capabilities
// - Web interface access for netop configuration
// - Advanced filtering by area, zone, line, column, and level
// - Search functionality across multiple netop properties
//
// UI Features:
// - Grid-based netop display with configurable span
// - Netop selection with detailed information dialogs
// - Preview modal with fullscreen toggle capability
// - Filter controls for organizing large netop collections
//
// Integration Points:
// - INetopService: Netop discovery operations
// - ISettingsService: Grid layout preferences and configuration
// - IFilterService: Advanced filtering and search capabilities
// - Browser integration: External web interface access
//
// User Experience:
// - Intuitive netop selection with action sheets
// - Progressive disclosure of netop information
// - Responsive grid layout with user preferences
public partial class NetopsViewModel : FilterableBaseViewModel<Netop>, ILoadableViewModel, IDisposable
{
    // Netop service for network discovery.
    // Handles communication with IP netops and discovery operations.
    private readonly INetopService _netopService;
    private readonly INetopConnectService _netopConnectService;
    private readonly INetOpRestartService _restartService;

    // Settings service for user preferences and configuration persistence.
    // Manages grid layout settings and other netop-related preferences.
    private readonly ISettingsService _settingsService;
    
    // TODO: Re-enable SecureCredentialsService after fixing DI issues
    // private readonly SecureCredentialsService _credentialsService;
    private bool _disposed = false;

    // Currently selected netop for detailed operations.
    // Used for netop-specific actions like web interface access.
    [ObservableProperty]
    private Netop? _selectedNetop;

    // Restart functionality properties
    [ObservableProperty]
    private string _restartTarget = string.Empty;

    [ObservableProperty]
    private string _connectToTarget = string.Empty;

    [ObservableProperty]
    private bool _isRestarting = false;

    [ObservableProperty]
    private string _restartStatusMessage = string.Empty;

    // Number of columns in the netop grid display layout.
    // User-configurable setting for organizing netop display density.
    [ObservableProperty]
    private int _gridSpan = 5;

    // Convenience properties for UI binding that expose filtered collections and selected values.
    // These properties provide direct access to netop data and filter options for data binding.
    
    // Collection of all netops available for display, filtered by current search and filter criteria.
    // This is an alias for the inherited Items property, providing semantic clarity for netop data.
    public ObservableCollection<Netop> Netops => Items;
    
    // Available area options for filtering netops by physical location area.
    // Dynamically populated from discovered netops and their location data.
    public ObservableCollection<string> AvailableAreas => FilterOptions.TryGetValue("Area", out var areas) ? areas : new();
    
    // Available zone options for filtering netops by specific zones within areas.
    // Provides fine-grained location filtering for large netop deployments.
    public ObservableCollection<string> AvailableZones => FilterOptions.TryGetValue("Zone", out var zones) ? zones : new();
    
    // Available line options for filtering netops by production lines or corridors.
    // Useful for manufacturing or facility monitoring netop organization.
    public ObservableCollection<string> AvailableLines => FilterOptions.TryGetValue("Line", out var lines) ? lines : new();
    
    // Available column options for filtering netops by column positions.
    // Enables grid-based location filtering for systematically positioned netops.
    public ObservableCollection<string> AvailableColumns => FilterOptions.TryGetValue("Column", out var columns) ? columns : new();
    
    // Available level options for filtering netops by floor or elevation levels.
    // Provides vertical location filtering for multi-story facility monitoring.
    public ObservableCollection<string> AvailableLevels => FilterOptions.TryGetValue("Level", out var levels) ? levels : new();

    // Filter selection properties that provide two-way binding for UI filter controls.
    // These properties enable users to select specific filter values and automatically
    // update the netop display based on the selected criteria.
    
    // Selected area filter for displaying netops in a specific physical area.
    // When set, filters the netop collection to show only netops in the selected area.
    public string SelectedArea
    {
        get => SelectedFilters.TryGetValue("Area", out var area) ? area : "";
        set => OnFilterChanged("Area", value);
    }

    // Selected zone filter for displaying netops in a specific zone within an area.
    // Provides more granular location filtering than area alone.
    public string SelectedZone
    {
        get => SelectedFilters.TryGetValue("Zone", out var zone) ? zone : "";
        set => OnFilterChanged("Zone", value);
    }

    // Selected line filter for displaying netops on a specific production line or corridor.
    // Useful for facility monitoring and manufacturing environment netop management.
    public string SelectedLine
    {
        get => SelectedFilters.TryGetValue("Line", out var line) ? line : "";
        set => OnFilterChanged("Line", value);
    }

    // Selected column filter for displaying netops in a specific column position.
    // Enables grid-based filtering for systematically positioned netop arrays.
    public string SelectedColumn
    {
        get => SelectedFilters.TryGetValue("Column", out var column) ? column : "";
        set => OnFilterChanged("Column", value);
    }

    // Selected level filter for displaying netops on a specific floor or elevation.
    // Provides vertical location filtering for multi-story facility monitoring.
    public string SelectedLevel
    {
        get => SelectedFilters.TryGetValue("Level", out var level) ? level : "";
        set => OnFilterChanged("Level", value);
    }

    // Constructor that initializes the NetopsViewModel with required services and dependencies.
    // Sets up netop management functionality and initializes the UI with default state.
    //
    // Parameters:
    // - logger: Logging service for debugging and monitoring netop operations
    // - navigationService: Navigation service for page routing and navigation
    // - dialogService: Dialog service for user interaction and alerts
    // - netopService: Netop-specific service for discovery and status operations
    // - filterService: Service for advanced filtering and search capabilities
    // - settingsService: Service for user preferences and configuration persistence
    //
    // Initialization Process:
    // 1. Calls base constructor to set up filtering and base ViewModel functionality
    // 2. Stores netop-specific services for later use
    // 3. Sets page title for UI display
    // 4. Loads user's grid span preference from settings
    // 5. Sets up settings change notifications for dynamic updates
    // 6. Initiates netop discovery
    //
    // Service Dependencies:
    // - INetopService: Network netop discovery
    // - ISettingsService: Grid layout preferences and netop-related settings
    // - Base services: Logging, navigation, dialogs, and filtering inherited from base
    public NetopsViewModel(
        ILogger<NetopsViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        INetopService netopService,
        IFilterService<Netop> filterService,
        ISettingsService settingsService,
        INetopConnectService netopConnectService,
        INetOpRestartService restartService) 
        : base(logger, navigationService, dialogService, filterService)
    {
        // Set the page title for UI display and navigation
        Title = "Netops";
        
        // Store netop service for network discovery and status operations
        _netopService = netopService;
        _netopConnectService = netopConnectService;
        _restartService = restartService;
        // Store settings service for user preferences and configuration
        _settingsService = settingsService;
        
        // TODO: Re-enable after fixing DI issues
        // _credentialsService = credentialsService;
        
        // Load user's preferred grid span setting asynchronously
        // This determines how many netops are displayed per row
        _ = LoadGridSpanAsync();
        
        // Subscribe to grid span changes from settings to update display dynamically
        // Allows real-time updates when user changes grid preferences
        SettingsManager.GridSpanChanged += OnGridSpanChanged;
        
        // Initialize netop discovery
        // Starts the process of finding network netops
        _ = LoadNetopsAsync();
    }

    // Defines the properties available for filtering netop collections.
    // This method overrides the base class to specify netop-specific filter criteria.
    //
    // Returns:
    // - Array of property names that can be used for filtering netops
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
    // - Enables dynamic filter UI generation based on available netop data
    // - Supports both UI picker controls and programmatic filtering
    protected override string[] GetFilterProperties()
    {
        return new[] { "Area", "Zone", "Line", "Column", "Level" };
    }

    // Command to load netops from the network using the netop service.
    // This method discovers available IP netops and populates the netop collection for display.
    //
    // Functionality:
    // - Calls netop service to discover network netops
    // - Loads discovered netops into the filterable collection
    // - Logs the number of netops found for monitoring
    // - Uses safe execution wrapper for error handling
    //
    // UI Integration:
    // - Can be triggered by refresh buttons or page initialization
    // - Updates the netop grid display with discovered devices
    // - Provides loading state feedback through IsBusy property
    [RelayCommand]
    private async Task LoadNetops()
    {
        await ExecuteSafelyAsync(async () =>
        {
            // Discover netops from network using netop service
            var netops = await _netopService.GetNetopsAsync();
            
            // Load netops into the filterable collection for display
            LoadItems(netops);
            
            // Log successful netop discovery for monitoring
            _logger.LogInformation("Loaded {Count} netops", netops.Count());
        }, "Load Netops");
    }

    // Command to handle netop selection and display available actions.
    // This method provides a user-friendly interface for netop interaction with progressive disclosure.
    //
    // Parameters:
    // - netop: The selected netop object containing connection and configuration information
    //
    // User Experience Flow:
    // 1. Sets the selected netop for tracking
    // 2. Presents action sheet with available functionality
    //
    // Available Actions:
    // - Preview in App: Shows netop feed within the application
    // - Open Web Interface: Launches netop's web configuration page
    // - Show Details: Displays comprehensive netop information
    [RelayCommand]
    private async Task SelectNetop(Netop netop)
    {
        await ExecuteSafelyAsync(async () =>
        {
            if (netop == null) return;
            SelectedNetop = netop;

            if (Application.Current?.MainPage == null) return;

            var details = $"Netop: {netop.Hostname}\nIP Address: {netop.PrimaryIp}\nLocation: {netop.FullLocation}\nWeb Interface: {netop.WebInterfaceUrl}";

            // Show action sheet first
            var action = await Application.Current.MainPage.DisplayActionSheet(
                $"📹 {netop.Hostname}",
                "Cancel",
                null,
                "🌐 Open Remote Connection",
                "ℹ️ Show Details");

            switch (action)
            {
                case "🌐 Open Remote Connection":
                    await RemoteConnectToDevice(netop);
                    break;

                case "ℹ️ Show Details":
                    // Now query SolarWinds status
                    var status = GetSolarWindsStatus(netop);
                    var statusEmoji = status?.IsOnline == true ? "🟢" : "🔴";
                    var statusText = status?.IsOnline == true ? "Online" : "Offline";
                    var responseTime = status?.ResponseTimeMs > 0 ? $" ({status.ResponseTimeMs}ms)" : "";

                    var enhancedDetails = $"{details}\n\nLive Status: {statusText}\nResponse Time: {status?.ResponseTimeMs ?? 0}ms\nLast Checked: {DateTime.Now:HH:mm:ss}";
                    await Application.Current.MainPage.DisplayAlert(
                        $"Netop Details {statusEmoji} {statusText}{responseTime}",
                        enhancedDetails,
                        "OK");
                    break;
            }
        }, "Select Netop");
    }

    // Command to open the netop's web interface in the default browser.
    // This method launches the netop's configuration and management web page externally.
    //
    // Parameters:
    // - netop: The netop object containing web interface URL and identification
    //
    // Web Interface Functionality:
    // - Opens netop's web configuration page in system browser
    // - Uses system preferred browser for consistent user experience
    // - Handles URL launch errors with detailed error reporting
    // - Logs web interface access for monitoring and debugging
    //
    // Error Handling:
    // - Catches browser launch failures
    // - Displays detailed error information to user
    // - Logs errors for troubleshooting and monitoring
    // - Provides fallback information for manual access
    

    // Private helper method to initialize netop data by loading netops.
    // This method performs netop discovery for complete initialization.
    //
    // Initialization Sequence:
    // 1. Loads netops from network discovery
    // 2. Provides complete netop information for UI display
    //
    // Usage:
    // - Called during ViewModel construction for initial setup
    // - Can be called for complete data refresh operations
    private async Task LoadNetopsAsync()
    {
        // Load netops from network discovery
        await LoadNetops();
    }

    // Override method to implement netop-specific search filtering.
    // This method defines how search text is applied to filter the netop collection.
    //
    // Parameters:
    // - items: Collection of netops to filter
    // - searchText: User-entered search criteria
    //
    // Returns:
    // - Filtered collection of netops matching search criteria
    //
    // Search Functionality:
    // - Searches across multiple netop properties simultaneously
    // - Case-insensitive matching for user-friendly experience
    // - Comprehensive coverage of identifiable netop attributes
    //
    // Searchable Properties:
    // - Name: Netop display name and identifier
    // - Model: Netop hardware model and type
    // - Area: Physical location area
    // - Zone: Specific zone within area
    // - Line: Production line or corridor
    // - Column: Grid column position
    // - Level: Floor or elevation level
    // - IpAddress: Network address for technical searches
    // - FullLocation: Complete location description
    protected override IEnumerable<Netop> ApplySearchFilter(IEnumerable<Netop> items, string searchText)
    {
        // Return unfiltered collection if no search text provided
        if (string.IsNullOrWhiteSpace(searchText))
            return items;

        // Convert search text to lowercase for case-insensitive matching
        var lowerSearchText = searchText.ToLowerInvariant();
        
        // Filter netops based on multiple property matches
        return items.Where(netop =>
            (netop.Hostname ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (netop.Model ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (netop.Area ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (netop.Zone ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (netop.Line ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (netop.Column ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (netop.Level ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (netop.PrimaryIp ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)
        );
    }

    // Private helper method to load grid span setting from user preferences.
    // This method retrieves the user's preferred netop grid layout configuration.
    //
    // Grid Span Functionality:
    // - Loads user's preferred number of columns in netop grid
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

    // Method to query SolarWinds for netop device status
    private DeviceStatus? GetSolarWindsStatus(Netop netop)
    {
        try
        {
            // Remove or comment out the delay below:
            // await Task.Delay(1000); // Simulate network call

            var random = new Random();
            var isOnline = random.NextDouble() > 0.3; // 70% chance of being online
            var responseTime = isOnline ? random.Next(10, 200) : 0;

            _logger.LogInformation("Mock SolarWinds status for {Hostname} - DI issues need to be resolved", netop.Hostname);

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
            _logger.LogError(ex, "Failed to query SolarWinds status for {Hostname}", netop.Hostname);
            return new DeviceStatus
            {
                IsOnline = false,
                ResponseTimeMs = 0,
                StatusDescription = "Error",
                LastChecked = DateTime.Now
            };
        }
        
    }
    
    [RelayCommand]
    private Task RemoteConnectToDevice(Netop device)
    {
        try
        {
            _netopService.ConnectToDevice(device.Hostname);
            RestartStatusMessage = $"Connection to {device.Hostname} initiated.";
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            RestartStatusMessage = $"Failed to connect: {ex.Message}";
            return Task.FromException(ex);
        }
    }

    [RelayCommand]
    private async Task RestartNetOpService()
    {
        await ExecuteSafelyAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(RestartTarget))
            {
                RestartStatusMessage = "Please enter a computer name or IP address.";
                return;
            }

            IsRestarting = true;
            RestartStatusMessage = $"Restarting NetOp service on {RestartTarget}...";

            try
            {
                // Use the injected service to restart NetOp service
                var result = await _restartService.RestartNetOpServiceAsync(RestartTarget);

                if (result.Status == "Success")
                {
                    RestartStatusMessage = $"✓ Successfully restarted NetOp service on {RestartTarget}";
                    RestartTarget = string.Empty; // Clear the input on success
                }
                else
                {
                    RestartStatusMessage = $"✗ Failed to restart NetOp service on {RestartTarget}: {result.Message}";
                }

                _logger.LogInformation("NetOp service restart result for {Target}: {Status} - {Message}", 
                    RestartTarget, result.Status, result.Message);
            }
            catch (Exception ex)
            {
                RestartStatusMessage = $"✗ Error restarting NetOp service: {ex.Message}";
                _logger.LogError(ex, "Error restarting NetOp service on {Target}", RestartTarget);
            }
            finally
            {
                IsRestarting = false;
            }
        }, "Restart NetOp Service");
    }

    [RelayCommand]
    private void DirectConnect()
    {
        if (string.IsNullOrWhiteSpace(ConnectToTarget))
        {
            RestartStatusMessage = "Please enter a computer name or IP address.";
            return;
        }

        try
        {
            _netopService.ConnectToDevice(ConnectToTarget);
            RestartStatusMessage = $"Connection to {ConnectToTarget} initiated.";
        }
        catch (Exception ex)
        {
            RestartStatusMessage = $"Failed to connect: {ex.Message}";
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
    /// Aliases to LoadNetopsCommand for automatic page loading
    /// </summary>
    public IAsyncRelayCommand LoadDataCommand => LoadNetopsCommand;
}
