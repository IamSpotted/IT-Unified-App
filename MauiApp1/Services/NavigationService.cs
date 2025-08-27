namespace MauiApp1.Services;

/// <summary>
/// Navigation service implementation following SOLID principles
/// </summary>
public class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;

    public NavigationService(ILogger<NavigationService> logger)
    {
        _logger = logger;
    }

    public async Task NavigateToAsync(string route)
    {
        try
        {
            _logger.LogInformation("Navigating to: {Route}", route);
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed to: {Route}", route);
            throw;
        }
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
    {
        try
        {
            _logger.LogInformation("Navigating to: {Route} with parameters", route);
            await Shell.Current.GoToAsync(route, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed to: {Route}", route);
            throw;
        }
    }

    public async Task GoBackAsync()
    {
        try
        {
            _logger.LogInformation("Navigating back");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Go back navigation failed");
            throw;
        }
    }

    public async Task PopToRootAsync()
    {
        try
        {
            _logger.LogInformation("Popping to root");
            await Shell.Current.GoToAsync("//");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pop to root navigation failed");
            throw;
        }
    }
}
