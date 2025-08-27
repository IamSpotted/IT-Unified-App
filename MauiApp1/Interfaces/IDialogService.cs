namespace MauiApp1.Interfaces;

/// <summary>
/// Interface for dialog service following Single Responsibility Principle
/// </summary>
public interface IDialogService : ITransientService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task<string> ShowPromptAsync(string title, string message, string placeholder = "", string initialValue = "", string accept = "OK", string cancel = "Cancel");
    Task ShowLoadingAsync(string message);
    Task HideLoadingAsync();
}
