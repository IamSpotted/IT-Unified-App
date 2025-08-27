using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class DnsLookupPage : ContentPage
{
    public DnsLookupPage(DnsLookupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Focus the query entry when the page appears
        QueryEntry.Focus();
    }
}
