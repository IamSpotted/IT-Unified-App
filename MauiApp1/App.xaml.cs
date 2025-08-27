namespace MauiApp1;

// Main application class that serves as the entry point for the MAUI application.
// This class manages the application lifecycle, window creation, and overall app initialization.
// It bridges the platform-specific startup code with the cross-platform application logic.
//
// Architecture:
// - Inherits from Application for MAUI framework integration
// - Creates and manages the main application window
// - Integrates with AppShell for navigation and routing
// - Handles application-wide events and state management
//
// Lifecycle:
// 1. Constructor called during app startup after MauiProgram.CreateMauiApp()
// 2. CreateWindow called when the platform needs to display the app
// 3. AppShell becomes the main content providing navigation structure
//
// Platform Integration:
// - Windows: Creates a desktop window with the shell content
// - iOS/Android: Creates a full-screen app with shell navigation
// - macOS: Creates a native macOS window with shell content
public partial class App : Application
{
	// Constructor initializes the application and loads XAML resources.
	// Called once during application startup after dependency injection is configured.
	// This is where application-wide resources, themes, and converters are loaded from App.xaml.
	public App()
	{
		// Initialize XAML components, load resources, and set up application-wide elements
		// This loads converters, styles, and theme resources defined in App.xaml
		InitializeComponent();
	}

	// Creates the main application window when the platform requests it.
	// This method is called by the MAUI framework when the app needs to be displayed.
	// Returns a Window containing the AppShell which provides the navigation structure.
	//
	// Parameters:
	// - activationState: Platform-specific activation information (launch args, etc.)
	//
	// Window Creation Process:
	// 1. Gets AppShell instance from dependency injection (includes role-based navigation)
	// 2. Wraps AppShell in a Window for platform display
	// 3. Platform renders the window with shell content
	// 4. Shell manages page navigation and routing within the window
	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Get AppShell from dependency injection container
		// This ensures proper RBAC integration and ViewModel binding
		var appShell = Handler?.MauiContext?.Services.GetRequiredService<AppShell>();
		
		// Create main application window with AppShell as the root content
		// AppShell provides navigation, routing, and role-based menu visibility
		return new Window(appShell ?? new AppShell(null!));
	}
}