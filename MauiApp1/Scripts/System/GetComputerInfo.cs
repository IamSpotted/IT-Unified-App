// Originally written in PowerShell by [Thomas Blake]
// Converted to C# by Jon Uldrick for integration with the MAUI IT Support Framework

using MauiApp1.Interfaces;
using MauiApp1.Views;

namespace MauiApp1.Scripts
{
    public class GetComputerInfo : IScript
    {
        public string ScriptName => "Get Computer Info";
        public string Description => "Gets computer info for specified hostnames/IPs. Supports local and remote computers.";
        public string Category => "System";
        public string Author => "Thomas Blake";

        public async void Execute()
        {
            try
            {
                // Navigate directly to the Computer Info page
                await Shell.Current.GoToAsync(nameof(ComputerInfoPage));
            }
            catch (Exception ex)
            {
                // Fallback to alert if navigation fails
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open Computer Info page: {ex.Message}", "OK");
                }
            }
        }
    }
}