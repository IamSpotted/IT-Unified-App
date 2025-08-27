namespace MauiApp1.Services;

/// <summary>
/// Theme service implementation following SOLID principles
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private readonly ISettingsService _settingsService;
    private const string ThemeSettingKey = "app_theme";

    public event EventHandler<AppTheme>? ThemeChanged;

    public bool IsDarkMode => Application.Current?.RequestedTheme == AppTheme.Dark;

    public ThemeService(ILogger<ThemeService> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        
        // Initialize with system theme by default
        _ = InitializeThemeAsync();
    }

    public async Task<AppTheme> GetCurrentThemeAsync()
    {
        try
        {
            var savedTheme = await _settingsService.GetAsync<string?>(ThemeSettingKey, "System");
            
            return savedTheme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified // System default
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current theme");
            return AppTheme.Unspecified;
        }
    }

    public async Task SetThemeAsync(AppTheme theme)
    {
        try
        {
            if (Application.Current == null) return;

            Application.Current.UserAppTheme = theme;
            
            var themeString = theme switch
            {
                AppTheme.Light => "Light",
                AppTheme.Dark => "Dark",
                _ => "System"
            };

            await _settingsService.SetAsync(ThemeSettingKey, themeString);
            
            ThemeChanged?.Invoke(this, theme);
            _logger.LogInformation("Theme changed to: {Theme}", themeString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set theme: {Theme}", theme);
            throw;
        }
    }

    private async Task InitializeThemeAsync()
    {
        try
        {
            var theme = await GetCurrentThemeAsync();
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = theme;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize theme");
        }
    }
}
