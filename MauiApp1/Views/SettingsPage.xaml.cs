using System.Timers;

namespace MauiApp1.Views;

// Application settings page that provides user configuration and customization options.
// This page allows users to modify application behavior, appearance, and preferences
// within the IT support framework, including theme management and other settings.
//
// Architecture:
// - Inherits from ContentPage for standard page behavior
// - Implements IView interface for consistent view contract
// - Uses dependency injection for ViewModel integration
// - Follows MVVM pattern with BindingContext data binding
// - Includes direct event handlers for immediate UI feedback
//
// Settings Features:
// - Theme selection and customization (Light/Dark mode)
// - Application preferences and configuration options
// - User interface customization settings
// - IT support tool configuration parameters
// - Performance and behavior optimization settings
//
// Theme Management:
// - Direct theme toggle implementation for immediate visual feedback
// - Integration with MAUI's AppTheme system
// - Dynamic icon updates to reflect current theme state
// - Persistent theme preferences across app sessions
//
// User Interface:
// - Settings organized by category for easy navigation
// - Toggle controls for boolean preferences
// - Input fields for configurable parameters
// - Immediate visual feedback for setting changes
//
// Integration:
// - Works with SettingsViewModel for data management
// - Integrates with application-wide theme system
// - Connects to configuration persistence services
// - Affects application behavior across all pages
//
// Navigation:
// - Accessible through Shell navigation routing
// - Part of the main application navigation structure
// - Central location for all application configuration
public partial class SettingsPage : ContentPage, Interfaces.IView
{
    // Constructor that receives the SettingsViewModel through dependency injection.
    // This establishes the MVVM connection and prepares the page for settings management functionality.
    //
    // Parameters:
    // - viewModel: SettingsViewModel injected by the DI container
    //
    // ViewModel Responsibilities:
    // - Settings data management and persistence
    // - Configuration validation and default values
    // - Integration with settings services
    // - Data binding for settings controls
    // - Settings change notifications and updates
    //
    // Initialization Process:
    // 1. Calls InitializeComponent() to load XAML UI elements
    // 2. Sets BindingContext to enable data binding between UI and ViewModel
    // 3. Establishes connection for settings data and configuration management
    //
    // MVVM Integration:
    // - ViewModel handles data persistence and validation
    // - Page handles immediate UI feedback and user interactions
    // - Hybrid approach for optimal user experience
    // - Data binding for complex settings, events for simple toggles
    public SettingsPage(SettingsViewModel viewModel)
    {
        // Initialize XAML components and load UI elements from SettingsPage.xaml
        // This sets up all settings controls and configuration interface
        InitializeComponent();
        
        // Set ViewModel as BindingContext to enable data binding
        // This connects settings controls to data management and persistence
        BindingContext = viewModel;
    }
    
    // Event handler for theme toggle functionality that provides immediate visual feedback.
    // This method handles direct theme switching without going through the ViewModel,
    // allowing for instant UI response and improved user experience.
    //
    // Parameters:
    // - sender: The UI element that triggered the theme toggle (typically a Label with emoji icon)
    // - e: Event arguments containing interaction details
    //
    // Theme Toggle Logic:
    // 1. Determines current application theme state
    // 2. Calculates the opposite theme for toggling
    // 3. Applies new theme to Application.Current.UserAppTheme
    // 4. Updates the visual indicator (emoji icon) to reflect new state
    //
    // Theme States:
    // - Light Mode: Uses light colors, shows moon icon (🌙) for switching to dark
    // - Dark Mode: Uses dark colors, shows sun icon (☀️) for switching to light
    // - Unspecified: Defaults to dark mode for consistent behavior
    //
    // Visual Feedback:
    // - Immediate theme application across entire application
    // - Icon update provides clear indication of current state
    // - Theme changes persist across app sessions via MAUI framework
    //
    // Integration with AppTheme System:
    // - Uses MAUI's built-in theme management (Application.Current.UserAppTheme)
    // - Leverages AppThemeBinding resources defined in Colors.xaml
    // - Triggers automatic UI updates across all pages and controls
    private void OnThemeToggleTapped(object sender, EventArgs e)
    {
        // Get current theme from application, defaulting to Unspecified if null
        // RequestedTheme reflects the actual current theme state
        var currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Unspecified;
        
        // Determine new theme using pattern matching for clean logic
        // Light switches to Dark, Dark switches to Light, Unspecified defaults to Dark
        var newTheme = currentTheme switch
        {
            AppTheme.Light => AppTheme.Dark,   // Light mode → Dark mode
            AppTheme.Dark => AppTheme.Light,   // Dark mode → Light mode
            _ => AppTheme.Dark                 // Default to dark when unspecified
        };
        
        // Apply new theme to application if Application.Current is available
        // This triggers immediate theme change across entire application
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = newTheme;
        }
        
        // Update visual indicator (emoji icon) to reflect new theme state
        // Sun icon (☀️) in dark mode indicates "switch to light"
        // Moon icon (🌙) in light mode indicates "switch to dark"
        if (sender is Label themeLabel)
        {
            themeLabel.Text = newTheme == AppTheme.Dark ? "☀️" : "🌙";
        }
    }
}
