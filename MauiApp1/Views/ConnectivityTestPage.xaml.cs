using MauiApp1.ViewModels;

namespace MauiApp1.Views
{
    public partial class ConnectivityTestPage : ContentPage
    {
        public ConnectivityTestPage(ConnectivityTestViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
