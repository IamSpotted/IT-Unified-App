namespace MauiApp1;

// Main dashboard page that serves as the application's home screen.
// Displays network health summary cards, device statistics, and provides navigation
// through the integrated ribbon control.
//
// Architecture:
// - Implements MVVM pattern with MainPageViewModel
// - Uses dependency injection for ViewModel and RibbonView
// - Implements IView interface for consistent view contracts
// - Integrates RibbonView for unified navigation across the application
//
// Features:
// - Real-time device status monitoring (Online/Offline/Warning/Critical)
// - Network health summary with percentage calculations
// - Digital clock display with seven-segment style
// - Responsive grid layout for device statistics
public partial class MainPage : ContentPage, Interfaces.IView
{
	private readonly MainPageViewModel _viewModel;

	// Constructor with dependency injection for ViewModel and RibbonView.
	// The DI container automatically provides these dependencies based on
	// the auto-discovery service registration in MauiProgram.cs
	public MainPage(MainPageViewModel viewModel)
	{
		// Initialize the XAML components and UI elements
		InitializeComponent();
		
		// Store injected dependencies for use throughout the page lifecycle
		_viewModel = viewModel;
		
		// Bind the ViewModel to enable data binding between UI and business logic
		// This connects all {Binding} expressions in the XAML to ViewModel properties
		BindingContext = _viewModel;
		
		// Integrate the ribbon navigation control into the page layout
		// RibbonContainer is defined in MainPage.xaml as a ContentView placeholder
	}
}
