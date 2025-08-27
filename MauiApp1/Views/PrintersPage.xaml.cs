namespace MauiApp1.Views;

// Printers management page that displays and manages network printer devices.
// This page provides a dedicated interface for viewing, configuring, and monitoring
// network printers within the IT support framework, separate from other network devices.
//
// Architecture:
// - Inherits from ContentPage for standard page behavior
// - Implements IView interface for consistent view contract
// - Uses dependency injection for ViewModel integration
// - Follows MVVM pattern with BindingContext data binding
//
// Printer Management Features:
// - Display list of discovered network printers
// - Show printer status and availability information
// - Provide printer configuration and management tools
// - Monitor print queues and job status
// - Handle printer driver and connection issues
//
// User Interface:
// - List view of available network printers
// - Printer status indicators (online, offline, error states)
// - Quick access to printer management functions
// - Print queue monitoring and job management
// - Printer configuration and troubleshooting tools
//
// Printer Operations:
// - Printer discovery and enumeration
// - Status monitoring and health checks
// - Print queue management and job control
// - Printer configuration and settings access
// - Driver installation and troubleshooting support
//
// Navigation:
// - Accessible through Shell navigation routing
// - Part of the main application navigation structure
// - Focused on printer-specific IT support workflows
//
// Integration:
// - Works with printer discovery services
// - Integrates with Windows print spooler system
// - Connects to printer web interfaces where available
// - Separate from general network device management (NetopsPage)
public partial class PrintersPage : ContentPage, Interfaces.IView
{
    // Constructor that receives the PrintersViewModel through dependency injection.
    // This establishes the MVVM connection and prepares the page for printer management functionality.
    //
    // Parameters:
    // - viewModel: PrintersViewModel injected by the DI container
    //
    // ViewModel Responsibilities:
    // - Network printer discovery and enumeration
    // - Printer status monitoring and health checks
    // - Print queue management and job monitoring
    // - Printer configuration and settings management
    // - Driver installation support and troubleshooting
    //
    // Initialization Process:
    // 1. Calls InitializeComponent() to load XAML UI elements
    // 2. Sets BindingContext to enable data binding between UI and ViewModel
    // 3. Establishes connection for printer discovery and management operations
    //
    // Printer Management Capabilities:
    // - Real-time printer status updates
    // - Print job queue monitoring and control
    // - Printer configuration access and modification
    // - Driver and connection troubleshooting tools
    //
    // MVVM Benefits:
    // - Separates printer management logic from UI presentation
    // - Enables testable printer operations code
    // - Supports data binding for real-time printer status updates
    // - Allows for clean separation of printer-specific concerns
    public PrintersPage(PrintersViewModel viewModel)
    {
        // Initialize XAML components and load UI elements from PrintersPage.xaml
        // This sets up the printer list and management interface
        InitializeComponent();
        
        // Set ViewModel as BindingContext to enable data binding
        // This connects UI elements to printer data, status updates, and management commands
        BindingContext = viewModel;
    }

    private PrintersViewModel? ViewModel => BindingContext as PrintersViewModel;

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
