using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class DeviceDetailsPopup : ContentPage
{
    public DeviceDetailsPopup(DeviceDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
