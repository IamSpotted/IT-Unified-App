using System.Windows.Input;

namespace MauiApp1.Controls;

public partial class BackButton : ContentView
{
    public static readonly BindableProperty BackTextProperty =
        BindableProperty.Create(nameof(BackText), typeof(string), typeof(BackButton), "← Back");

    public static readonly BindableProperty BackRouteProperty =
        BindableProperty.Create(nameof(BackRoute), typeof(string), typeof(BackButton), string.Empty);

    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(BackButton), null);

    public BackButton()
    {
        InitializeComponent();
        
        // Set default command if none provided
        BackCommand = new Command(async () => await ExecuteBackNavigation());
    }

    public string BackText
    {
        get => (string)GetValue(BackTextProperty);
        set => SetValue(BackTextProperty, value);
    }

    public string BackRoute
    {
        get => (string)GetValue(BackRouteProperty);
        set => SetValue(BackRouteProperty, value);
    }

    public ICommand BackCommand
    {
        get => (ICommand)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    private async Task ExecuteBackNavigation()
    {
        try
        {
            if (!string.IsNullOrEmpty(BackRoute))
            {
                // Navigate to specific route
                await Shell.Current.GoToAsync(BackRoute);
            }
            else
            {
                // Default back navigation
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            // Fallback navigation
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Navigation Error", 
                    $"Could not navigate back: {ex.Message}", "OK");
            }
        }
    }
}
