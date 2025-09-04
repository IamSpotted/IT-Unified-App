using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Interfaces;

namespace MauiApp1.ViewModels;

public partial class DeviceDetailsViewModel : ObservableObject, IQueryAttributable
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Models.Device _device = new();

    public DeviceDetailsViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("Device") && query["Device"] is Models.Device device)
        {
            Device = device;
        }
    }

    [RelayCommand]
    private async Task Close()
    {
        await _navigationService.GoBackAsync();
    }
}
