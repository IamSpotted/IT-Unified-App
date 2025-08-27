namespace MauiApp1.Interfaces;

/// <summary>
/// Interface for application settings following Single Responsibility Principle
/// </summary>
/// 
public interface ISettingsService : ISingletonService
{
    Task<T?> GetAsync<T>(string key, T? defaultValue = default);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
    Task ClearAllAsync();
}
