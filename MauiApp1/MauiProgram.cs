using MauiApp1.Extensions;
using MauiApp1.Views;
using MauiApp1.ViewModels;
using CommunityToolkit.Maui;

namespace MauiApp1;

// Main application entry point and dependency injection configuration.
// This class follows SOLID principles by using auto-discovery for service registration,
// making the application modular and maintainable.
//
// Architecture Pattern: Clean Architecture with MVVM
// - Services are auto-discovered using marker interfaces
// - View-ViewModel mappings are automatically registered  
// - Logging is configured based on build configuration
public static class MauiProgram
{
	// Creates and configures the MAUI application with all necessary services.
	// This is the main entry point called by the MAUI framework on startup.
	// Returns: Configured MauiApp instance ready for execution
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		// Configure core MAUI application and fonts
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Configure services and logging using auto-discovery pattern
		builder.Services.ConfigureServices();
		builder.Services.ConfigureLogging();

		return builder.Build();
	}

	// Configures all application services using auto-discovery and SOLID principles.
	// Services are automatically registered based on marker interfaces defined in ServiceMarkers.cs.
	// This approach promotes loose coupling and makes adding new services effortless.
	//
	// Auto-Discovery Process:
	// 1. Scans assembly for classes implementing marker interfaces (ITransientService, IScopedService, ISingletonService)
	// 2. Automatically registers services with appropriate lifetime
	// 3. Registers View-ViewModel mappings for MVVM navigation
	//
	// Benefits:
	// - No manual service registration required for new services
	// - Consistent lifetime management
	// - Follows SOLID Open/Closed principle
	private static IServiceCollection ConfigureServices(this IServiceCollection services)
	{
		// Auto-discover and register all services based on marker interfaces
		services.AddAutoDiscoveredServices();
		
		// Register View-ViewModel pairs for MVVM navigation
		services.AddViewViewModelMappings();

		// Register AppShell with dependency injection
		services.AddSingleton<AppShell>();

		// Register Computer Info page and ViewModel
		services.AddTransient<ComputerInfoPage>();
		services.AddTransient<ComputerInfoViewModel>();

		// Register Connectivity Test page and ViewModel
		services.AddTransient<ConnectivityTestPage>();
		services.AddTransient<ConnectivityTestViewModel>();

		// Register Restart NetOp page and ViewModel
		services.AddTransient<RestartNetOpPage>();
		services.AddTransient<RestartNetOpViewModel>();

		// Register Documentation page and ViewModel
		services.AddTransient<DocumentationPage>();
		services.AddTransient<DocumentationViewModel>();

		// Register Multi-Ping Dashboard page and ViewModel
		services.AddTransient<MultiPingDashboardPage>();
		services.AddSingleton<MultiPingViewModel>(); // Singleton to maintain session state

		// Register DNS Lookup page and ViewModel
		services.AddTransient<DnsLookupPage>();
		services.AddTransient<DnsLookupViewModel>();

		// Register Ping Session Detail page and ViewModel
		services.AddTransient<PingSessionDetailPage>();
		services.AddTransient<PingSessionDetailViewModel>();

		// Register Device Details popup and ViewModel
		services.AddTransient<DeviceDetailsPopup>();
		services.AddTransient<DeviceDetailsViewModel>();

		// Register Netop Connect service
		services.AddSingleton<INetopConnectService, NetopConnectService>();

		// Manual service registrations can be added here if needed for special cases
		// Example: services.AddTransient<ISpecificService, SpecificServiceImplementation>();

		return services;
	}

	// Configures logging services based on the build configuration.
	// Debug builds include debug output logging, while Release builds use standard logging.
	//
	// Logging Configuration:
	// - DEBUG: Includes debug output for development and troubleshooting
	// - RELEASE: Standard logging only for production performance
	//
	// Usage in Services:
	// Services can inject ILogger<TService> for structured logging throughout the application.
	private static IServiceCollection ConfigureLogging(this IServiceCollection services)
	{
#if DEBUG
		// Debug builds: Add debug output and file logging for development
		services.AddLogging(builder => 
		{
			builder.AddDebug();
			// Add file logging to easily track database connections
			var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MauiApp1", "logs");
			Directory.CreateDirectory(logPath);
			var logFile = Path.Combine(logPath, $"app-{DateTime.Now:yyyy-MM-dd}.log");
			
			builder.AddProvider(new FileLoggerProvider(logFile));
		});
#else
		// Release builds: Standard logging configuration for production
		services.AddLogging();
#endif
		return services;
	}
}
