namespace MauiApp1.ViewModels;

// Base ViewModel class that provides common functionality for all ViewModels in the application.
// This abstract class implements the MVVM pattern with SOLID principles, offering shared services,
// error handling, and common properties that all ViewModels need within the IT support framework.
//
// Architecture:
// - Inherits from ObservableObject for property change notifications
// - Implements IViewModel interface for consistent ViewModel contract
// - Uses partial class for CommunityToolkit.Mvvm source generation
// - Provides dependency injection for common services
//
// SOLID Principles Implementation:
// - Single Responsibility: Handles common ViewModel concerns
// - Open/Closed: Extensible through inheritance, closed for modification
// - Liskov Substitution: All derived ViewModels can substitute this base
// - Interface Segregation: Uses focused service interfaces
// - Dependency Inversion: Depends on abstractions (interfaces) not concretions
//
// Common Services Provided:
// - ILogger: Centralized logging for debugging and monitoring
// - INavigationService: Page navigation and routing management
// - IDialogService: User dialog and alert management
//
// Shared Properties:
// - IsBusy: Indicates when async operations are running
// - Title: Page or section title for UI binding
//
// Error Handling:
// - Centralized exception handling with logging
// - User-friendly error dialog display
// - Safe async operation execution with busy state management
//
// Usage Pattern:
// - All ViewModels inherit from this base class
// - Provides consistent behavior across the application
// - Reduces code duplication and ensures proper error handling
public abstract partial class BaseViewModel : ObservableObject, IViewModel
{
    // Core services injected into all ViewModels for common functionality.
    // These services provide essential capabilities needed across the application.

    // Logger service for debugging, monitoring, and error tracking.
    // Used to record application events, errors, and diagnostic information.
    protected readonly ILogger _logger;

    // Navigation service for page routing and navigation management.
    // Handles programmatic navigation between pages in the Shell navigation system.
    protected readonly INavigationService _navigationService;

    // Dialog service for user interaction through alerts, confirmations, and messages.
    // Provides consistent dialog experience across all ViewModels.
    protected readonly IDialogService _dialogService;

    // Indicates whether the ViewModel is currently performing an async operation.
    // Used to prevent multiple simultaneous operations and provide UI feedback.
    // The [ObservableProperty] attribute generates PropertyChanged notifications automatically.
    [ObservableProperty]
    private bool _isBusy;

    // Title property for page or section identification in the UI.
    // Used for data binding to page titles, headers, or navigation breadcrumbs.
    // The [ObservableProperty] attribute generates PropertyChanged notifications automatically.
    [ObservableProperty]
    private string _title = string.Empty;

    // Constructor that initializes the base ViewModel with essential services.
    // All derived ViewModels must call this constructor to receive common services.
    //
    // Parameters:
    // - logger: ILogger for error tracking and diagnostic information
    // - navigationService: INavigationService for page navigation management
    // - dialogService: IDialogService for user interaction and alerts
    //
    // Dependency Injection:
    // - Services are injected by the DI container during ViewModel creation
    // - Ensures all ViewModels have access to essential application services
    // - Promotes loose coupling and testability through interface dependencies
    //
    // Service Initialization:
    // - Stores service references for use throughout ViewModel lifecycle
    // - Services remain available for all ViewModel operations
    // - Enables consistent error handling and user interaction patterns
    protected BaseViewModel(
        ILogger logger,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        // Store logger service for error tracking and diagnostic logging
        _logger = logger;

        // Store navigation service for page routing and navigation operations
        _navigationService = navigationService;

        // Store dialog service for user alerts and interaction management
        _dialogService = dialogService;
    }

    // Centralized exception handling method that logs errors and displays user-friendly messages.
    // This method provides consistent error handling behavior across all ViewModels.
    //
    // Parameters:
    // - ex: The exception that occurred during operation execution
    // - operation: Descriptive name of the operation that failed (for logging and user display)
    //
    // Error Handling Process:
    // 1. Logs the exception with operation context for debugging
    // 2. Displays user-friendly error message via dialog service
    // 3. Provides consistent error experience across the application
    //
    // Logging Benefits:
    // - Captures detailed exception information for debugging
    // - Includes operation context for better error tracking
    // - Enables monitoring and diagnostics in production
    //
    // User Experience:
    // - Shows friendly error messages instead of technical details
    // - Provides actionable feedback ("Please try again")
    // - Maintains consistent dialog appearance and behavior
    protected virtual async Task HandleExceptionAsync(Exception ex, string operation = "Operation")
    {
        // Log detailed exception information with operation context
        // This captures the full exception details for debugging and monitoring
        _logger.LogError(ex, "{Operation} failed", operation);

        // Display user-friendly error message through dialog service
        // Shows operation name and generic retry instruction
        await _dialogService.ShowAlertAsync("Error", $"{operation} failed. Please try again.");
    }

    // Safe execution wrapper for async operations that don't return values.
    // This method provides consistent error handling and busy state management for void operations.
    //
    // Parameters:
    // - operation: The async operation to execute safely
    // - operationName: Descriptive name for logging and error reporting
    //
    // Safety Features:
    // - Prevents multiple simultaneous operations by checking IsBusy
    // - Manages IsBusy state automatically (sets to true during execution)
    // - Handles exceptions with centralized error handling
    // - Ensures IsBusy is reset even if operation fails
    //
    // Execution Flow:
    // 1. Checks if already busy and returns early if so
    // 2. Sets IsBusy to true to prevent concurrent operations
    // 3. Executes the provided operation
    // 4. Handles any exceptions through HandleExceptionAsync
    // 5. Always resets IsBusy to false in finally block
    //
    // UI Benefits:
    // - IsBusy property can be bound to loading indicators
    // - Prevents users from triggering multiple operations
    // - Provides consistent loading state management
    protected async Task ExecuteSafelyAsync(Func<Task> operation, string operationName = "Operation")
    {
        // Return early if already busy to prevent concurrent operations
        if (IsBusy) return;

        try
        {
            // Set busy state to indicate operation in progress
            // This can be bound to UI loading indicators
            IsBusy = true;

            // Execute the provided async operation
            await operation();
        }
        catch (Exception ex)
        {
            // Handle any exceptions using centralized error handling
            // Logs error and shows user-friendly message
            await HandleExceptionAsync(ex, operationName);
        }
        finally
        {
            // Always reset busy state, even if operation failed
            // Ensures UI returns to normal state
            IsBusy = false;
        }
    }

    // Safe execution wrapper for async operations that return values.
    // This method provides consistent error handling and busy state management for operations with return values.
    //
    // Parameters:
    // - operation: The async operation to execute safely that returns type T
    // - defaultValue: Value to return if operation fails (default is default(T))
    // - operationName: Descriptive name for logging and error reporting
    //
    // Return Value:
    // - Result of the operation if successful
    // - Default value if operation fails or ViewModel is busy
    //
    // Safety Features:
    // - Prevents multiple simultaneous operations by checking IsBusy
    // - Manages IsBusy state automatically during execution
    // - Handles exceptions with centralized error handling
    // - Returns safe default value on failure
    // - Ensures IsBusy is reset even if operation fails
    //
    // Generic Type Support:
    // - Works with any return type T
    // - Nullable return types supported for reference types
    // - Flexible default value specification
    protected async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, T? defaultValue = default, string operationName = "Operation")
    {
        // Return default value if already busy to prevent concurrent operations
        if (IsBusy) return defaultValue;

        try
        {
            // Set busy state to indicate operation in progress
            // This can be bound to UI loading indicators
            IsBusy = true;

            // Execute the provided async operation and return its result
            return await operation();
        }
        catch (Exception ex)
        {
            // Handle any exceptions using centralized error handling
            // Logs error and shows user-friendly message
            await HandleExceptionAsync(ex, operationName);

            // Return default value when operation fails
            return defaultValue;
        }
        finally
        {
            // Always reset busy state, even if operation failed
            // Ensures UI returns to normal state
            IsBusy = false;
        }
    }
    
    public virtual void Dispose()
    {
        // Dispose of any resources if needed
        // This can be overridden in derived ViewModels for custom cleanup
        // Default implementation does nothing
    }
}
