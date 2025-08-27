using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class MultiPingDashboardPage : ContentPage
{
    public MultiPingDashboardPage(MultiPingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Focus the target entry when the page appears
        TargetEntry.Focus();
    }
}
