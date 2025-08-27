using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Interfaces;
using MauiApp1.Services;
using Microsoft.Extensions.Logging;

namespace MauiApp1.ViewModels
{
    /// <summary>
    /// Simple Scripts ViewModel - based on working console app pattern
    /// </summary>
    public partial class SimpleScriptsViewModel : ObservableObject, ITransientService
    {
        private readonly ISimpleScriptService _scriptService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<SimpleScriptsViewModel> _logger;

        [ObservableProperty]
        private ObservableCollection<IScript> _scripts = new();

        [ObservableProperty]
        private IScript? _selectedScript;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = "Simple Scripts";

        public SimpleScriptsViewModel(
            ISimpleScriptService scriptService,
            IDialogService dialogService,
            ILogger<SimpleScriptsViewModel> logger)
        {
            _scriptService = scriptService;
            _dialogService = dialogService;
            _logger = logger;

            LoadScripts();
        }

        [RelayCommand]
        private void LoadScripts()
        {
            try
            {
                IsBusy = true;
                Scripts.Clear();

                var discoveredScripts = _scriptService.GetAllScripts();
                _logger.LogInformation($"Loading {discoveredScripts.Count} scripts");

                foreach (var script in discoveredScripts)
                {
                    Scripts.Add(script);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load scripts");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ExecuteScript(IScript? script)
        {
            if (script == null) return;

            try
            {
                var confirmResult = await _dialogService.ShowConfirmationAsync(
                    "Execute Script",
                    $"Execute '{script.ScriptName}'?\n\n{script.Description}",
                    "Execute",
                    "Cancel");

                if (confirmResult)
                {
                    IsBusy = true;
                    _logger.LogInformation($"Executing script: {script.ScriptName}");

                    // Execute script on background thread
                    await Task.Run(() => _scriptService.ExecuteScript(script));

                    await _dialogService.ShowAlertAsync(
                        "Script Completed",
                        $"Script '{script.ScriptName}' executed successfully!",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to execute script: {script.ScriptName}");
                await _dialogService.ShowAlertAsync(
                    "Script Error",
                    $"Failed to execute '{script.ScriptName}':\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void RefreshScripts()
        {
            LoadScripts();
        }
    }
}
