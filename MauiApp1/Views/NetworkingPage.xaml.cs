using MauiApp1.ViewModels;

namespace MauiApp1.Views;

// Network Tools page that provides access to network troubleshooting and diagnostic tools.
// This page serves as a central hub for network-related functionality, including connectivity
// testing, network diagnostics, and quick access to common network commands.
//
// Architecture:
// - Inherits from ContentPage for standard page behavior
// - Implements IView interface for consistent view contract
// - Uses dependency injection for ViewModel integration
// - Follows MVVM pattern with BindingContext data binding
//
// Network Tools Features:
// - Connectivity Test: Real-time ping testing with live statistics and response time analysis
// - Network Diagnostics: Comprehensive network configuration analysis using scripts
// - Quick Actions: Fast access to IP configuration, DNS lookup, and network statistics
// - User-friendly card-based interface for easy navigation
//
// User Interface:
// - Card-based layout for different network tools
// - Quick action buttons for common network commands
// - Information section explaining available tools
// - Consistent styling with other application pages
//
// Navigation Integration:
// - Accessible through Shell navigation routing as "networking"
// - Part of the main application navigation structure
// - Provides navigation to connectivity testing and diagnostic tools
public partial class NetworkingPage : ContentPage, Interfaces.IView
{
    public NetworkingPage(NetworkingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
