using MauiApp1.Views;
using MauiApp1.ViewModels;

namespace MauiApp1;

// Application shell that defines the overall navigation structure and routing system.
// This class serves as the main navigation container for the entire application,
// managing page transitions and URL-based routing for different application sections.
//
// Architecture:
// - Inherits from Shell for MAUI's modern navigation framework
// - Registers route mappings for programmatic navigation
// - Works in conjunction with RibbonView for UI-based navigation
// - Enables deep linking and URI-based navigation throughout the app
//
// Navigation Structure:
// - cameras: Camera monitoring and management page
// - netops: Network operations and device monitoring page  
// - networking: Network configuration and tools page
// - printers: Printer monitoring and management page
// - scripts: Script execution and automation page
// - settings: Application configuration and preferences page
//
// Usage:
// Navigate programmatically: await Shell.Current.GoToAsync("//cameras")
// Navigate with parameters: await Shell.Current.GoToAsync("//netops?filter=offline")
public partial class AppShell : Shell
{
	// Constructor initializes the shell and sets up all navigation routes.
	// Called once during application startup to establish the navigation framework.
	public AppShell(AppShellViewModel viewModel)
	{
		// Initialize XAML components and shell structure
		InitializeComponent();
		
		// Set the BindingContext to enable role-based menu visibility
		BindingContext = viewModel;
		
		// Register all application routes for programmatic navigation
		RegisterRoutes();
	}

	// Registers route mappings between string identifiers and page types.
	// This enables navigation using simple string routes instead of type references,
	// supporting both programmatic navigation and potential deep linking scenarios.
	//
	// Route Registration Benefits:
	// - Clean separation between navigation logic and page types
	// - Enables URI-based navigation and deep linking
	// - Supports parameterized navigation (e.g., "cameras?type=security")
	// - Facilitates testing by allowing route mocking
	private static void RegisterRoutes()
	{
		// Register each application page with its corresponding route identifier
		// These routes can be used with Shell.Current.GoToAsync() for navigation
		Routing.RegisterRoute("cameras", typeof(CamerasPage));
		Routing.RegisterRoute("netops", typeof(NetopsPage));
		Routing.RegisterRoute("networking", typeof(NetworkingPage));
		Routing.RegisterRoute("printers", typeof(PrintersPage));
		Routing.RegisterRoute("scripts", typeof(ScriptsPage));
		Routing.RegisterRoute("settings", typeof(SettingsPage));
		Routing.RegisterRoute("database-admin", typeof(DatabaseAdminPage));
		Routing.RegisterRoute("network-devices", typeof(NetworkDevicesPage));

		// Register script-specific pages
		Routing.RegisterRoute(nameof(ComputerInfoPage), typeof(ComputerInfoPage));
		Routing.RegisterRoute(nameof(ConnectivityTestPage), typeof(ConnectivityTestPage));
		Routing.RegisterRoute(nameof(RestartNetOpPage), typeof(RestartNetOpPage));
		
		// Register multi-ping dashboard
		Routing.RegisterRoute(nameof(MultiPingDashboardPage), typeof(MultiPingDashboardPage));
		
		// Register DNS lookup page
		Routing.RegisterRoute(nameof(DnsLookupPage), typeof(DnsLookupPage));
		
		// Register ping session detail page
		Routing.RegisterRoute(nameof(PingSessionDetailPage), typeof(PingSessionDetailPage));
		
		// Register device details popup
		Routing.RegisterRoute(nameof(DeviceDetailsPopup), typeof(DeviceDetailsPopup));
	}
}
