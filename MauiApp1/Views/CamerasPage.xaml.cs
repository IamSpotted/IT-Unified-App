namespace MauiApp1.Views;

// Cameras management page that displays and manages network camera devices.
// This page provides a user interface for viewing, configuring, and monitoring IP cameras
// within the IT support framework. It follows the MVVM pattern with data binding to CamerasViewModel.
//
// Architecture:
// - Inherits from ContentPage for standard page behavior
// - Implements IView interface for consistent view contract
// - Uses dependency injection for ViewModel integration
// - Follows MVVM pattern with BindingContext data binding
//
// User Interface:
// - Displays list of available network cameras
// - Provides camera status monitoring capabilities
// - Allows camera configuration and management
// - Integrates with filtering and search functionality
//
// Navigation:
// - Accessible through Shell navigation routing
// - Part of the main application navigation structure
// - Can be navigated to via code or UI navigation
//
// Data Management:
// - Loads camera data asynchronously when page appears
// - Updates camera status in real-time
// - Handles camera discovery and configuration
public partial class CamerasPage : ContentPage, Interfaces.IView
{
    // Strongly-typed access to the page's ViewModel for camera operations.
    // This property casts the BindingContext to CamerasViewModel, providing
    // type-safe access to camera-specific functionality and data.
    //
    // ViewModel provides:
    // - LoadCamerasCommand for async camera discovery
    // - Camera collection for UI binding
    // - Filtering and search capabilities
    // - Camera status monitoring
    private CamerasViewModel ViewModel => (CamerasViewModel)BindingContext;

    // Constructor that receives the CamerasViewModel through dependency injection.
    // This establishes the MVVM connection and prepares the page for camera management.
    //
    // Parameters:
    // - viewModel: CamerasViewModel injected by the DI container
    //
    // Initialization Process:
    // 1. Calls InitializeComponent() to load XAML UI elements
    // 2. Sets BindingContext to enable data binding between UI and ViewModel
    // 3. Establishes two-way communication for camera data and commands
    public CamerasPage(CamerasViewModel viewModel)
    {
        // Initialize XAML components and load UI elements from CamerasPage.xaml
        InitializeComponent();
        
        // Set ViewModel as BindingContext to enable data binding
        // This connects UI elements to ViewModel properties and commands
        BindingContext = viewModel;
    }

    // Lifecycle method called when the page becomes visible to the user.
    // This triggers camera discovery and data loading to ensure current information
    // is displayed when the user navigates to the cameras page.
    //
    // Async Operations:
    // - Loads available cameras from network discovery
    // - Updates camera status and configuration
    // - Refreshes the camera list for current state
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
