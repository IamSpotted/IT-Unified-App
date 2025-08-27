using MauiApp1.Interfaces;

namespace MauiApp1.Views;

/// <summary>
/// Base page class that provides automatic data loading functionality
/// </summary>
/// <typeparam name="TViewModel">The ViewModel type that implements ILoadableViewModel</typeparam>
public abstract class BaseLoadablePage<TViewModel> : ContentPage 
    where TViewModel : class, ILoadableViewModel
{
    /// <summary>
    /// Gets the ViewModel from the BindingContext
    /// </summary>
    protected TViewModel? ViewModel => BindingContext as TViewModel;

    /// <summary>
    /// Called when the page appears - automatically loads data
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load data when page appears
        if (ViewModel != null)
        {
            await ViewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Called when the page disappears - disposes the ViewModel if needed
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Dispose ViewModel when page is unloaded to clean up event subscriptions
        if (BindingContext is IDisposable disposableViewModel)
        {
            disposableViewModel.Dispose();
        }
    }
}
