using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class DatabaseAdminPage : ContentPage, Interfaces.IView
{
    private DatabaseAdminViewModel? ViewModel => BindingContext as DatabaseAdminViewModel;

    public DatabaseAdminPage(DatabaseAdminViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load data when page appears using the standardized interface
        if (ViewModel != null)
        {
            await ViewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }

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
