using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class RestartNetOpPage : ContentPage
{
    public RestartNetOpPage(RestartNetOpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}