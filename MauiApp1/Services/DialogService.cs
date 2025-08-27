namespace MauiApp1.Services;

/// <summary>
/// Dialog service implementation following SOLID principles
/// </summary>
public class DialogService : IDialogService
{
    private readonly ILogger<DialogService> _logger;
    private Page MainPage => Application.Current?.MainPage ?? throw new InvalidOperationException("MainPage not available");

    public DialogService(ILogger<DialogService> logger)
    {
        _logger = logger;
    }

    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        try
        {
            _logger.LogInformation("Showing alert: {Title}", title);
            await MainPage.DisplayAlert(title, message, cancel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show alert: {Title}", title);
            throw;
        }
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        try
        {
            _logger.LogInformation("Showing confirmation: {Title}", title);
            return await MainPage.DisplayAlert(title, message, accept, cancel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show confirmation: {Title}", title);
            throw;
        }
    }

    public async Task<string> ShowPromptAsync(string title, string message, string placeholder = "", string initialValue = "", string accept = "OK", string cancel = "Cancel")
    {
        try
        {
            _logger.LogInformation("Showing prompt: {Title}", title);
            return await MainPage.DisplayPromptAsync(title, message, accept, cancel, placeholder, -1, null, initialValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show prompt: {Title}", title);
            throw;
        }
    }

    public async Task ShowLoadingAsync(string message)
    {
        try
        {
            _logger.LogInformation("Showing loading: {Message}", message);
            // Implementation would depend on your preferred loading indicator
            // For now, just log it - you could integrate with a loading popup or overlay
            await Task.Delay(50); // Simulate async operation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show loading");
            throw;
        }
    }

    public async Task HideLoadingAsync()
    {
        try
        {
            _logger.LogInformation("Hiding loading");
            // Implementation would depend on your preferred loading indicator
            await Task.Delay(50); // Simulate async operation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide loading");
            throw;
        }
    }
}
