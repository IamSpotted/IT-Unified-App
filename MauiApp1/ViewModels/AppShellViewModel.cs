using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    private readonly IAuthorizationService _authorizationService;

    [ObservableProperty]
    private bool _showDatabaseAdmin = false;

    [ObservableProperty]
    private bool _showAdvancedFeatures = false;

    [ObservableProperty]
    private string _currentUserDisplayName = string.Empty;

    [ObservableProperty]
    private string _currentUserRole = string.Empty;

    public AppShellViewModel(
        ILogger<AppShellViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        IAuthorizationService authorizationService)
        : base(logger, navigationService, dialogService)
    {
        _authorizationService = authorizationService;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            await UpdateMenuVisibilityAsync();
            await UpdateUserInfoAsync();
        }, "Initialize AppShell");
    }

    public async Task UpdateMenuVisibilityAsync()
    {
        try
        {
            // Check permissions for menu items
            ShowDatabaseAdmin = await _authorizationService.CanAccessDatabaseAdminAsync();
            ShowAdvancedFeatures = await _authorizationService.HasPermissionAsync(Permission.SystemConfiguration);

            _logger.LogInformation("Menu visibility updated - DatabaseAdmin: {DatabaseAdmin}, Advanced: {Advanced}", 
                ShowDatabaseAdmin, ShowAdvancedFeatures);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update menu visibility");
            // Default to hiding sensitive menu items on error
            ShowDatabaseAdmin = false;
            ShowAdvancedFeatures = false;
        }
    }

    private async Task UpdateUserInfoAsync()
    {
        try
        {
            CurrentUserDisplayName = await _authorizationService.GetCurrentUserNameAsync();
            var userRole = await _authorizationService.GetUserRoleAsync();
            CurrentUserRole = userRole.ToString();

            _logger.LogDebug("User info updated - Name: {Name}, Role: {Role}", 
                CurrentUserDisplayName, CurrentUserRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user info");
            CurrentUserDisplayName = "Unknown User";
            CurrentUserRole = "Unknown";
        }
    }

    [RelayCommand]
    private async Task NavigateToDatabaseAdmin()
    {
        // Double-check permissions before navigation
        if (!await _authorizationService.CanAccessDatabaseAdminAsync())
        {
            await _dialogService.ShowAlertAsync("Access Denied", 
                "You don't have permission to access Database Administration.\n\n" +
                $"Your current role: {CurrentUserRole}\n" +
                "Required role: Database Admin or System Admin");
            return;
        }

        await _navigationService.NavigateToAsync("//database-admin");
    }

    [RelayCommand]
    private async Task RefreshPermissions()
    {
        await ExecuteSafelyAsync(async () =>
        {
            // Clear authorization cache and refresh
            _authorizationService.ClearCache();
            await UpdateMenuVisibilityAsync();
            await UpdateUserInfoAsync();
            
            _logger.LogInformation("Permissions refreshed");
        }, "Refresh Permissions");
    }
}
