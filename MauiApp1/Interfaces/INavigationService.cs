namespace MauiApp1.Interfaces;

/// <summary>
/// Interface for navigation service following Single Responsibility Principle
/// </summary>
public interface INavigationService : ITransientService
{
    Task NavigateToAsync(string route);
    Task NavigateToAsync(string route, IDictionary<string, object> parameters);
    Task GoBackAsync();
    Task PopToRootAsync();
}
