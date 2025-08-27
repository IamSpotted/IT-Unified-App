using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Text;
using System.Windows.Input;
using MauiApp1.Models;
using MauiApp1.Interfaces;

namespace MauiApp1.ViewModels
{
    public class RestartNetOpViewModel : INotifyPropertyChanged, IViewModel
    {
        private readonly INetOpRestartService _restartService;
        private string _targetInput = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isProcessing = false;
        private bool _hasResults = false;
        private ObservableCollection<NetOpRestartResult> _restartResults = new();

        public RestartNetOpViewModel(INetOpRestartService restartService)
        {
            _restartService = restartService;
            RestartCommand = new Command(async () => await RestartNetOpAsync());
            ClearCommand = new Command(ClearResults);
            CopyResultsCommand = new Command(async () => await CopyResultsAsync());
        }

        public string TargetInput
        {
            get => _targetInput;
            set
            {
                _targetInput = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
            }
        }

        public bool HasResults
        {
            get => _hasResults;
            set
            {
                _hasResults = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<NetOpRestartResult> RestartResults
        {
            get => _restartResults;
            set
            {
                _restartResults = value;
                OnPropertyChanged();
            }
        }

        public ICommand RestartCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand CopyResultsCommand { get; }

        private async Task RestartNetOpAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetInput))
            {
                StatusMessage = "Please enter at least one computer name or IP address.";
                return;
            }

            IsProcessing = true;
            HasResults = false;
            RestartResults.Clear();

            try
            {
                // Parse computer names
                var computers = TargetInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                StatusMessage = $"Restarting NetOp services on {computers.Count} computer(s)...";

                // Use the injected service to restart NetOp services
                var results = await _restartService.RestartNetOpServicesAsync(computers);

                // Add results to the collection
                foreach (var result in results)
                {
                    RestartResults.Add(result);
                }

                HasResults = RestartResults.Count > 0;
                var successCount = RestartResults.Count(r => r.Status == "Success");
                StatusMessage = $"Completed: {successCount}/{RestartResults.Count} successful restarts";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }


        private void ClearResults()
        {
            RestartResults.Clear();
            HasResults = false;
            StatusMessage = string.Empty;
        }

        private async Task CopyResultsAsync()
        {
            try
            {
                var results = new StringBuilder();
                results.AppendLine("NetOp Service Restart Results");
                results.AppendLine("================================");
                results.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                results.AppendLine();

                foreach (var result in RestartResults)
                {
                    results.AppendLine($"Computer: {result.ComputerName}");
                    results.AppendLine($"Status: {result.Status}");
                    results.AppendLine($"Message: {result.Message}");
                    
                    if (result.HasDetails)
                    {
                        results.AppendLine("Details:");
                        results.AppendLine(result.Details);
                    }
                    
                    if (result.IsCompleted)
                    {
                        results.AppendLine($"Completed: {result.Timestamp:HH:mm:ss}");
                    }
                    
                    results.AppendLine(new string('-', 40));
                }

                await Clipboard.Default.SetTextAsync(results.ToString());
                StatusMessage = "Results copied to clipboard";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error copying results: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
