namespace MauiApp1.Services;

/// <summary>
/// Static settings manager for app-wide settings that need to be shared across ViewModels
/// </summary>
public static class SettingsManager
{
    public static event EventHandler<int>? GridSpanChanged;
    public static event EventHandler<string>? FontSizeChanged;

    public static void NotifyGridSpanChanged(int newGridSpan)
    {
        GridSpanChanged?.Invoke(null, newGridSpan);
    }

    public static void NotifyFontSizeChanged(string newFontSize)
    {
        FontSizeChanged?.Invoke(null, newFontSize);
    }
}
