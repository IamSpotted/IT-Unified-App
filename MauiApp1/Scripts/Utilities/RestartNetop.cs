using MauiApp1.Interfaces;
using MauiApp1.Views;

namespace MauiApp1.Scripts
{
    public class RestartNetOp : IScript
    {
        public string ScriptName => "Restart NetOp";
        public string Description => "Restarts the Netops/Impero service on the specified host.";
        public string Category => "Utilities";
        public string Author => "Thomas Blake";

        public async void Execute()
        {
            try
            {
                // Navigate to the dedicated Restart NetOp page
                await Shell.Current.GoToAsync(nameof(RestartNetOpPage));
            }
            catch (Exception ex)
            {
                // Fallback to alert if navigation fails
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open Restart NetOp page: {ex.Message}", "OK");
                }
            }
        }
    }
}