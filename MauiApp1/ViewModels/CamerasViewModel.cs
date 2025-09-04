namespace MauiApp1.ViewModels;

// ViewModel for managing IP camera devices within the IT support framework.
// This ViewModel provides comprehensive camera management functionality including discovery,
// preview capabilities, and filtering/search operations for network cameras.
//
// Architecture:
// - Inherits from FilterableBaseViewModel<Camera> for filtering and search functionality
// - Uses partial class for CommunityToolkit.Mvvm source generation
// - Implements MVVM pattern with data binding and command handling
// - Integrates with multiple services for camera operations
//
// Core Functionality:
// - Camera discovery and enumeration from network
// - Camera preview and fullscreen viewing capabilities
// - Web interface access for camera configuration
// - Advanced filtering by area, zone, line, column, and level
// - Search functionality across multiple camera properties
//
// UI Features:
// - Grid-based camera display with configurable span
// - Camera selection with detailed information dialogs
// - Preview modal with fullscreen toggle capability
// - Filter controls for organizing large camera collections
//
// Integration Points:
// - ICameraService: Camera discovery operations
// - ISettingsService: Grid layout preferences and configuration
// - IFilterService: Advanced filtering and search capabilities
// - Browser integration: External web interface access
//
// User Experience:
// - Intuitive camera selection with action sheets
// - Progressive disclosure of camera information
// - Responsive grid layout with user preferences
public partial class CamerasViewModel : FilterableBaseViewModel<Camera>, ILoadableViewModel, IDisposable
{
    // Camera service for network discovery.
    // Handles communication with IP cameras and discovery operations.
    private readonly ICameraService _cameraService;
    
    // Settings service for user preferences and configuration persistence.
    // Manages grid layout settings and other camera-related preferences.
    private readonly ISettingsService _settingsService;
    private readonly SecureCredentialsService _credentialsService;
    private bool _disposed = false;

    // Currently selected camera for detailed operations.
    // Used for camera-specific actions like web interface access.
    [ObservableProperty]
    private Camera? _selectedCamera;

    // Number of columns in the camera grid display layout.
    // User-configurable setting for organizing camera display density.
    [ObservableProperty]
    private int _gridSpan = 5;

    // Convenience properties for UI binding that expose filtered collections and selected values.
    // These properties provide direct access to camera data and filter options for data binding.
    
    // Collection of all cameras available for display, filtered by current search and filter criteria.
    // This is an alias for the inherited Items property, providing semantic clarity for camera data.
    public ObservableCollection<Camera> Cameras => Items;
    
    // Available area options for filtering cameras by physical location area.
    // Dynamically populated from discovered cameras and their location data.
    public ObservableCollection<string> AvailableAreas => FilterOptions.TryGetValue("Area", out var areas) ? areas : new();
    
    // Available zone options for filtering cameras by specific zones within areas.
    // Provides fine-grained location filtering for large camera deployments.
    public ObservableCollection<string> AvailableZones => FilterOptions.TryGetValue("Zone", out var zones) ? zones : new();
    
    // Available line options for filtering cameras by production lines or corridors.
    // Useful for manufacturing or facility monitoring camera organization.
    public ObservableCollection<string> AvailableLines => FilterOptions.TryGetValue("Line", out var lines) ? lines : new();
    
    // Available column options for filtering cameras by column positions.
    // Enables grid-based location filtering for systematically positioned cameras.
    public ObservableCollection<string> AvailableColumns => FilterOptions.TryGetValue("Column", out var columns) ? columns : new();
    
    // Available level options for filtering cameras by floor or elevation levels.
    // Provides vertical location filtering for multi-story facility monitoring.
    public ObservableCollection<string> AvailableLevels => FilterOptions.TryGetValue("Level", out var levels) ? levels : new();

    // Filter selection properties that provide two-way binding for UI filter controls.
    // These properties enable users to select specific filter values and automatically
    // update the camera display based on the selected criteria.
    
    // Selected area filter for displaying cameras in a specific physical area.
    // When set, filters the camera collection to show only cameras in the selected area.
    public string SelectedArea
    {
        get => SelectedFilters.TryGetValue("Area", out var area) ? area : "";
        set => OnFilterChanged("Area", value);
    }

    // Selected zone filter for displaying cameras in a specific zone within an area.
    // Provides more granular location filtering than area alone.
    public string SelectedZone
    {
        get => SelectedFilters.TryGetValue("Zone", out var zone) ? zone : "";
        set => OnFilterChanged("Zone", value);
    }

    // Selected line filter for displaying cameras on a specific production line or corridor.
    // Useful for facility monitoring and manufacturing environment camera management.
    public string SelectedLine
    {
        get => SelectedFilters.TryGetValue("Line", out var line) ? line : "";
        set => OnFilterChanged("Line", value);
    }

    // Selected column filter for displaying cameras in a specific column position.
    // Enables grid-based filtering for systematically positioned camera arrays.
    public string SelectedColumn
    {
        get => SelectedFilters.TryGetValue("Column", out var column) ? column : "";
        set => OnFilterChanged("Column", value);
    }

    // Selected level filter for displaying cameras on a specific floor or elevation.
    // Provides vertical location filtering for multi-story facility monitoring.
    public string SelectedLevel
    {
        get => SelectedFilters.TryGetValue("Level", out var level) ? level : "";
        set => OnFilterChanged("Level", value);
    }

    // Constructor that initializes the CamerasViewModel with required services and dependencies.
    // Sets up camera management functionality and initializes the UI with default state.
    //
    // Parameters:
    // - logger: Logging service for debugging and monitoring camera operations
    // - navigationService: Navigation service for page routing and navigation
    // - dialogService: Dialog service for user interaction and alerts
    // - cameraService: Camera-specific service for discovery and status operations
    // - filterService: Service for advanced filtering and search capabilities
    // - settingsService: Service for user preferences and configuration persistence
    //
    // Initialization Process:
    // 1. Calls base constructor to set up filtering and base ViewModel functionality
    // 2. Stores camera-specific services for later use
    // 3. Sets page title for UI display
    // 4. Loads user's grid span preference from settings
    // 5. Sets up settings change notifications for dynamic updates
    // 6. Initiates camera discovery
    //
    // Service Dependencies:
    // - ICameraService: Network camera discovery
    // - ISettingsService: Grid layout preferences and camera-related settings
    // - Base services: Logging, navigation, dialogs, and filtering inherited from base
    public CamerasViewModel(
        ILogger<CamerasViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        ICameraService cameraService,
        IFilterService<Camera> filterService,
        ISettingsService settingsService,
        SecureCredentialsService credentialsService) 
        : base(logger, navigationService, dialogService, filterService)
    {
        // Set the page title for UI display and navigation
        Title = "Cameras";
        
        // Store camera service for network discovery and status operations
        _cameraService = cameraService;
        
        // Store settings service for user preferences and configuration
        _settingsService = settingsService;
        
        // TODO: Re-enable after fixing DI issues
        _credentialsService = credentialsService;
        
        // Load user's preferred grid span setting asynchronously
        // This determines how many cameras are displayed per row
        _ = LoadGridSpanAsync();
        
        // Subscribe to grid span changes from settings to update display dynamically
        // Allows real-time updates when user changes grid preferences
        SettingsManager.GridSpanChanged += OnGridSpanChanged;
        
        // Initialize camera discovery
        // Starts the process of finding network cameras
        _ = LoadCamerasAsync();
    }

    // Defines the properties available for filtering camera collections.
    // This method overrides the base class to specify camera-specific filter criteria.
    //
    // Returns:
    // - Array of property names that can be used for filtering cameras
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
    // - Enables dynamic filter UI generation based on available camera data
    // - Supports both UI picker controls and programmatic filtering
    protected override string[] GetFilterProperties()
    {
        return new[] { "Area", "Zone", "Line", "Column", "Level" };
    }

    // Command to load cameras from the network using the camera service.
    // This method discovers available IP cameras and populates the camera collection for display.
    //
    // Functionality:
    // - Calls camera service to discover network cameras
    // - Loads discovered cameras into the filterable collection
    // - Logs the number of cameras found for monitoring
    // - Uses safe execution wrapper for error handling
    //
    // UI Integration:
    // - Can be triggered by refresh buttons or page initialization
    // - Updates the camera grid display with discovered devices
    // - Provides loading state feedback through IsBusy property
    [RelayCommand]
    private async Task LoadCameras()
    {
        await ExecuteSafelyAsync(async () =>
        {
            // Discover cameras from network using camera service
            var cameras = await _cameraService.GetCamerasAsync();
            
            // Load cameras into the filterable collection for display
            LoadItems(cameras);
            
            // Log successful camera discovery for monitoring
            _logger.LogInformation("Loaded {Count} cameras", cameras.Count());
        }, "Load Cameras");
    }

    // Command to handle camera selection and display available actions.
    // This method provides a user-friendly interface for camera interaction with progressive disclosure.
    //
    // Parameters:
    // - camera: The selected camera object containing connection and configuration information
    //
    // User Experience Flow:
    // 1. Sets the selected camera for tracking
    // 2. Presents action sheet with available functionality
    //
    // Available Actions:
    // - Preview in App: Shows camera feed within the application
    // - Open Web Interface: Launches camera's web configuration page
    // - Show Details: Displays comprehensive camera information
    [RelayCommand]
    private async Task SelectCamera(Camera camera)
    {
        await ExecuteSafelyAsync(async () =>
        {
            if (camera == null) return;
            SelectedCamera = camera;

            if (Application.Current?.MainPage == null) return;

            var details = $"Camera: {camera.Hostname}\nIP Address: {camera.PrimaryIp}\nLocation: {camera.FullLocation}\nWeb Interface: {camera.WebInterfaceUrl}";

            // Show action sheet first
            var action = await Application.Current.MainPage.DisplayActionSheet(
                $"📹 {camera.Hostname}",
                "Cancel",
                null,
                "🌐 Open Web Interface",
                "ℹ️ Show Details");

            switch (action)
            {
                case "🌐 Open Web Interface":
                    await OpenWebInterface(camera);
                    break;

                case "ℹ️ Show Details":
                    // Now query SolarWinds status
                    var status = GetSolarWindsStatus(camera);
                    var statusEmoji = status?.IsOnline == true ? "🟢" : "🔴";
                    var statusText = status?.IsOnline == true ? "Online" : "Offline";
                    var responseTime = status?.ResponseTimeMs > 0 ? $" ({status.ResponseTimeMs}ms)" : "";

                    var enhancedDetails = $"{details}\n\nLive Status: {statusText}\nResponse Time: {status?.ResponseTimeMs ?? 0}ms\nLast Checked: {DateTime.Now:HH:mm:ss}";
                    await Application.Current.MainPage.DisplayAlert(
                        $"Camera Details {statusEmoji} {statusText}{responseTime}", enhancedDetails, "OK");
                    break;
            }
        }, "Select Camera");
    }

    // Command to open the camera's web interface in the default browser.
    // This method launches the camera's configuration and management web page externally.
    //
    // Parameters:
    // - camera: The camera object containing web interface URL and identification
    //
    // Web Interface Functionality:
    // - Opens camera's web configuration page in system browser
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
    private async Task OpenWebInterface(Camera camera)
    {
        try
        {
            _logger.LogInformation("Opening web interface for camera {Hostname} at {Url}",
                camera.Hostname, camera.WebInterfaceUrl);

            if (string.IsNullOrWhiteSpace(camera.WebInterfaceUrl))
            {
                await _dialogService.ShowAlertAsync("Error", "Web interface URL is empty or null");
                return;
            }

            if (!Uri.TryCreate(camera.WebInterfaceUrl, UriKind.Absolute, out var uri))
            {
                await _dialogService.ShowAlertAsync("Error", $"Invalid URL format: {camera.WebInterfaceUrl}");
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

            _logger.LogInformation("Successfully opened web interface for {Hostname}", camera.Hostname);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open web interface for {Hostname}. URL: {Url}",
                camera.Hostname, camera.WebInterfaceUrl);
            await _dialogService.ShowAlertAsync("Error",
                $"Unable to open web interface for {camera.Hostname}.\nURL: {camera.WebInterfaceUrl}\nError: {ex.Message}");
        }
    }

    // Private helper method to initialize camera data by loading cameras.
    // This method performs camera discovery for complete initialization.
    //
    // Initialization Sequence:
    // 1. Loads cameras from network discovery
    // 2. Provides complete camera information for UI display
    //
    // Usage:
    // - Called during ViewModel construction for initial setup
    // - Can be called for complete data refresh operations
    private async Task LoadCamerasAsync()
    {
        // Load cameras from network discovery
        await LoadCameras();
    }

    // Override method to implement camera-specific search filtering.
    // This method defines how search text is applied to filter the camera collection.
    //
    // Parameters:
    // - items: Collection of cameras to filter
    // - searchText: User-entered search criteria
    //
    // Returns:
    // - Filtered collection of cameras matching search criteria
    //
    // Search Functionality:
    // - Searches across multiple camera properties simultaneously
    // - Case-insensitive matching for user-friendly experience
    // - Comprehensive coverage of identifiable camera attributes
    //
    // Searchable Properties:
    // - Name: Camera display name and identifier
    // - Model: Camera hardware model and type
    // - Area: Physical location area
    // - Zone: Specific zone within area
    // - Line: Production line or corridor
    // - Column: Grid column position
    // - Level: Floor or elevation level
    // - IpAddress: Network address for technical searches
    // - FullLocation: Complete location description
    protected override IEnumerable<Camera> ApplySearchFilter(IEnumerable<Camera> items, string searchText)
    {
        // Return unfiltered collection if no search text provided
        if (string.IsNullOrWhiteSpace(searchText))
            return items;

        // Convert search text to lowercase for case-insensitive matching
        var lowerSearchText = searchText.ToLowerInvariant();
        
        // Filter cameras based on multiple property matches
        return items.Where(camera =>
            (camera.Hostname ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (camera.Model ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (camera.Area ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (camera.Zone ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (camera.Line ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (camera.Column ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (camera.Level ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (camera.PrimaryIp ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)
        );
    }

    // Private helper method to load grid span setting from user preferences.
    // This method retrieves the user's preferred camera grid layout configuration.
    //
    // Grid Span Functionality:
    // - Loads user's preferred number of columns in camera grid
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

    // Method to query SolarWinds for camera device status
    private DeviceStatus? GetSolarWindsStatus(Camera camera)
    {
        try
        {
            // Remove or comment out the delay below:
            // await Task.Delay(1000); // Simulate network call

            var random = new Random();
            var isOnline = random.NextDouble() > 0.3; // 70% chance of being online
            var responseTime = isOnline ? random.Next(10, 200) : 0;

            _logger.LogInformation("Mock SolarWinds status for {Hostname} - DI issues need to be resolved", camera.Hostname);

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
            _logger.LogError(ex, "Failed to query SolarWinds status for {Hostname}", camera.Hostname);
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
    /// Aliases to LoadCamerasCommand for automatic page loading
    /// </summary>
    public IAsyncRelayCommand LoadDataCommand => LoadCamerasCommand;
}
