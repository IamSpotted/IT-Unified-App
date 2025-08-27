namespace MauiApp1.Interfaces;

/// <summary>
/// Interface for theme service following Single Responsibility Principle
/// </summary>
public interface IThemeService : ITransientService
{
    /// <summary>
    /// Gets the current theme mode
    /// </summary>
    Task<AppTheme> GetCurrentThemeAsync();

    /// <summary>
    /// Sets the theme mode
    /// </summary>
    Task SetThemeAsync(AppTheme theme);

    /// <summary>
    /// Gets whether the current theme is dark mode
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Event raised when theme changes
    /// </summary>
    event EventHandler<AppTheme> ThemeChanged;
}
