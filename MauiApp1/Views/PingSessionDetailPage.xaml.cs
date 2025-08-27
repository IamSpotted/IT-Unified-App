using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class PingSessionDetailPage : ContentPage
{
    public PingSessionDetailPage(PingSessionDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
