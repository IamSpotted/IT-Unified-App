using Microsoft.Maui.Storage;

namespace MauiApp1.Services;

/// <summary>
/// Settings service implementation using MAUI Preferences
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    private static bool IsPreferencesAvailable()
    {
#if ANDROID || IOS || WINDOWS || MACCATALYST
        return true;
#else
        return false;
#endif
    }

    public Task<T?> GetAsync<T>(string key, T? defaultValue = default)
    {
        if (key == "NetopServicePath")
            return Task.FromResult((T?)(object)@"C:\Program Files (x86)\Netop\Netop Remote Control\Guest\ngstw32.exe");

        if (!IsPreferencesAvailable())
        {
            _logger.LogWarning("Preferences is not available on this platform. Returning default value for {Key}", key);
            return Task.FromResult(defaultValue);
        }

        try
        {
            var value = Preferences.Get(key, string.Empty);

            if (string.IsNullOrEmpty(value))
                return Task.FromResult(defaultValue);

            var result = JsonSerializer.Deserialize<T>(value);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get setting: {Key}", key);
            return Task.FromResult(defaultValue);
        }
    }

    public Task SetAsync<T>(string key, T value)
    {
        if (!IsPreferencesAvailable())
        {
            _logger.LogWarning("Preferences is not available on this platform. Cannot save setting: {Key}", key);
            return Task.CompletedTask;
        }

        try
        {
            var json = JsonSerializer.Serialize(value);
            Preferences.Set(key, json);
            _logger.LogDebug("Setting saved: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save setting: {Key}", key);
            throw;
        }
    }

    public Task RemoveAsync(string key)
    {
        if (!IsPreferencesAvailable())
        {
            _logger.LogWarning("Preferences is not available on this platform. Cannot remove setting: {Key}", key);
            return Task.CompletedTask;
        }

        try
        {
            Preferences.Remove(key);
            _logger.LogDebug("Setting removed: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove setting: {Key}", key);
            throw;
        }
    }

    public Task ClearAllAsync()
    {
        if (!IsPreferencesAvailable())
        {
            _logger.LogWarning("Preferences is not available on this platform. Cannot clear settings.");
            return Task.CompletedTask;
        }

        try
        {
            Preferences.Clear();
            _logger.LogInformation("All settings cleared");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all settings");
            throw;
        }
    }
}
