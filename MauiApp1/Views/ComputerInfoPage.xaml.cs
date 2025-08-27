using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class ComputerInfoPage : ContentPage
{
    public ComputerInfoPage(ComputerInfoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
