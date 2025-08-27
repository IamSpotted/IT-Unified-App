namespace MauiApp1.ViewModels;

/// <summary>
/// Ribbon view model for navigation buttons
/// </summary>
public partial class RibbonViewModel : BaseViewModel
{
    public RibbonViewModel(
        ILogger<RibbonViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService) 
        : base(logger, navigationService, dialogService)
    {
        Title = "Navigation";
    }

    [RelayCommand]
    private async Task NavigateTo(string page)
    {
        await ExecuteSafelyAsync(async () =>
        {
            var route = page.ToLower() switch
            {
                "cameras" => "//cameras",
                "netops" => "//netops", 
                "networking" => "//networking",
                "printers" => "//printers",
                "scripts" => "//scripts",
                "settings" => "//settings",
                _ => "//main"
            };
            
            await _navigationService.NavigateToAsync(route);
            _logger.LogInformation("Navigated to {Page}", page);
        }, $"Navigate to {page}");
    }
}
