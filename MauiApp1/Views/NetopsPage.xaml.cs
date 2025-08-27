namespace MauiApp1.Views;

// Network Operations page that displays and manages network devices (excluding cameras and printers).
// This page provides access to network infrastructure devices like switches, routers, access points,
// and PC devices, allowing users to open web interfaces or establish remote connections through Netops.
//
// Architecture:
// - Inherits from ContentPage for standard page behavior
// - Implements IView interface for consistent view contract
// - Uses dependency injection for ViewModel integration
// - Follows MVVM pattern with BindingContext data binding
//
// Device Management Features:
// - Display network infrastructure devices (switches, routers, access points)
// - Show PC devices available for remote connection
// - Exclude cameras and printers (handled by dedicated pages)
// - Provide device status and connectivity information
//
// User Interface:
// - List view of discovered network devices
// - Device type identification and categorization
// - Action buttons for web interface access
// - Remote connection options for PC devices
// - Device status indicators and information
//
// Device Interaction:
// - Open web interfaces for network infrastructure (switches, routers, etc.)
// - Initiate Netops remote connections to PC devices
// - Display device information and connection status
// - Provide quick access to device management tools
//
// Navigation:
// - Accessible through Shell navigation routing
// - Part of the main application navigation structure
// - Focused on network device management workflows
//
// Integration:
// - Works with network discovery services
// - Integrates with Netops for remote connections
// - Connects to device web interfaces
// - Excludes devices managed by other pages (cameras/printers)
public partial class NetopsPage : ContentPage, Interfaces.IView
{
    // Constructor that receives the NetopsViewModel through dependency injection.
    // This establishes the MVVM connection and prepares the page for network device management functionality.
    //
    // Parameters:
    // - viewModel: NetopsViewModel injected by the DI container
    //
    // ViewModel Responsibilities:
    // - Network device discovery and enumeration (excluding cameras/printers)
    // - Device categorization (switches, routers, access points, PCs)
    // - Web interface URL management for infrastructure devices
    // - Netops remote connection handling for PC devices
    // - Device status monitoring and connectivity checks
    //
    // Initialization Process:
    // 1. Calls InitializeComponent() to load XAML UI elements
    // 2. Sets BindingContext to enable data binding between UI and ViewModel
    // 3. Establishes connection for device discovery and management operations
    //
    // Device Access Methods:
    // - Web interface launching for network infrastructure devices
    // - Netops remote connection initiation for PC devices
    // - Device information display and status updates
    // - Integration with external management tools
    //
    // MVVM Benefits:
    // - Separates device management logic from UI presentation
    // - Enables testable network device operations code
    // - Supports data binding for real-time device updates
    // - Allows for clean separation of device access concerns
    public NetopsPage(NetopsViewModel viewModel)
    {
        // Initialize XAML components and load UI elements from NetopsPage.xaml
        // This sets up the network device list and management interface
        InitializeComponent();
        
        // Set ViewModel as BindingContext to enable data binding
        // This connects UI elements to device data, web interface commands, and Netops connections
        BindingContext = viewModel;
    }

    private NetopsViewModel? ViewModel => BindingContext as NetopsViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load data when page appears using the standardized interface
        if (ViewModel != null)
        {
            await ViewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Dispose ViewModel when page is unloaded to clean up event subscriptions
        if (BindingContext is IDisposable disposableViewModel)
        {
            disposableViewModel.Dispose();
        }
    }
}
