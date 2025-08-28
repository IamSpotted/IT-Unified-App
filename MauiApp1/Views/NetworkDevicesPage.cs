namespace MauiApp1.Views;

// NetworkDevices management page that displays and manages network networkDevice devices.
// This page provides a user interface for viewing, configuring, and monitoring IP networkDevices
// within the IT support framework. It follows the MVVM pattern with data binding to NetworkDevicesViewModel.
//
// Architecture:
// - Inherits from ContentPage for standard page behavior
// - Implements IView interface for consistent view contract
// - Uses dependency injection for ViewModel integration
// - Follows MVVM pattern with BindingContext data binding
//
// User Interface:
// - Displays list of available network networkDevices
// - Provides networkDevice status monitoring capabilities
// - Allows networkDevice configuration and management
// - Integrates with filtering and search functionality
//
// Navigation:
// - Accessible through Shell navigation routing
// - Part of the main application navigation structure
// - Can be navigated to via code or UI navigation
//
// Data Management:
// - Loads networkDevice data asynchronously when page appears
// - Updates networkDevice status in real-time
// - Handles networkDevice discovery and configuration
public partial class NetworkDevicesPage : ContentPage, Interfaces.IView
{
    // Strongly-typed access to the page's ViewModel for networkDevice operations.
    // This property casts the BindingContext to NetworkDevicesViewModel, providing
    // type-safe access to networkDevice-specific functionality and data.
    //
    // ViewModel provides:
    // - LoadNetworkDevicesCommand for async networkDevice discovery
    // - NetworkDevice collection for UI binding
    // - Filtering and search capabilities
    // - NetworkDevice status monitoring
    private NetworkDevicesViewModel ViewModel => (NetworkDevicesViewModel)BindingContext;

    // Constructor that receives the NetworkDevicesViewModel through dependency injection.
    // This establishes the MVVM connection and prepares the page for networkDevice management.
    //
    // Parameters:
    // - viewModel: NetworkDevicesViewModel injected by the DI container
    //
    // Initialization Process:
    // 1. Calls InitializeComponent() to load XAML UI elements
    // 2. Sets BindingContext to enable data binding between UI and ViewModel
    // 3. Establishes two-way communication for networkDevice data and commands
    public NetworkDevicesPage(NetworkDevicesViewModel viewModel)
    {
        // Initialize XAML components and load UI elements from NetworkDevicesPage.xaml
        InitializeComponent();
        
        // Set ViewModel as BindingContext to enable data binding
        // This connects UI elements to ViewModel properties and commands
        BindingContext = viewModel;
    }

    // Lifecycle method called when the page becomes visible to the user.
    // This triggers networkDevice discovery and data loading to ensure current information
    // is displayed when the user navigates to the networkDevices page.
    //
    // Async Operations:
    // - Loads available networkDevices from network discovery
    // - Updates networkDevice status and configuration
    // - Refreshes the networkDevice list for current state
    //
    // Error Handling:
    // - ViewModel handles network timeouts and discovery failures
    // - UI shows appropriate loading states and error messages
    protected override async void OnAppearing()
    {
        // Call base implementation for standard page appearing behavior
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
        if (ViewModel is IDisposable disposableViewModel)
        {
            disposableViewModel.Dispose();
        }
    }
}
