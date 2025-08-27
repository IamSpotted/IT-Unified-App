using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MauiApp1.Models;

namespace MauiApp1.ViewModels
{
    public partial class ConnectivityTestViewModel : ObservableObject, IDisposable
    {
        private readonly ILogger<ConnectivityTestViewModel> _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        private Ping _pingSender = new();
        
        [ObservableProperty]
        private string _targetHost = "";
        
        [ObservableProperty]
        private string _selectedPingCount = "5";
        
        [ObservableProperty]
        private bool _isRunning = false;
        
        [ObservableProperty]
        private bool _hasResults = false;
        
        [ObservableProperty]
        private bool _hasResponseTimes = false;
        
        [ObservableProperty]
        private string _status = "Ready";
        
        [ObservableProperty]
        private string _statusMessage = "";
        
        [ObservableProperty]
        private int _packetsSent = 0;
        
        [ObservableProperty]
        private int _packetsReceived = 0;
        
        [ObservableProperty]
        private int _packetsLost = 0;
        
        [ObservableProperty]
        private double _lossPercentage = 0.0;
        
        [ObservableProperty]
        private long _minResponseTime = 0;
        
        [ObservableProperty]
        private long _maxResponseTime = 0;
        
        [ObservableProperty]
        private double _avgResponseTime = 0.0;
        
        public ObservableCollection<PingResult> PingResults { get; } = new();
        
        public ConnectivityTestViewModel(ILogger<ConnectivityTestViewModel> logger)
        {
            _logger = logger;
        }
        
        [RelayCommand]
        private async Task StartPing()
        {
            if (string.IsNullOrWhiteSpace(TargetHost))
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Please enter a target host.", "OK");
                }
                return;
            }
            
            // Reset everything
            PingResults.Clear();
            PacketsSent = 0;
            PacketsReceived = 0;
            PacketsLost = 0;
            LossPercentage = 0.0;
            MinResponseTime = 0;
            MaxResponseTime = 0;
            AvgResponseTime = 0.0;
            HasResponseTimes = false;
            
            IsRunning = true;
            HasResults = true;
            Status = "Running";
            
            // Parse ping count
            int pingCount = SelectedPingCount == "Continuous" ? -1 : int.Parse(SelectedPingCount);
            bool isContinuous = pingCount == -1;
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                await RunPingTest(TargetHost, pingCount, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Status = "Stopped";
                StatusMessage = "Ping test stopped by user";
            }
            catch (Exception ex)
            {
                Status = "Error";
                StatusMessage = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error during ping test");
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Ping test failed: {ex.Message}", "OK");
                }
            }
            finally
            {
                IsRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        
        [RelayCommand]
        private void StopPing()
        {
            _cancellationTokenSource?.Cancel();
        }
        
        private async Task RunPingTest(string target, int pingCount, CancellationToken cancellationToken)
        {
            bool isContinuous = pingCount == -1;
            int count = 0;
            long totalTime = 0;
            long minTime = long.MaxValue;
            long maxTime = 0;
            var responseTimes = new List<long>();
            
            StatusMessage = isContinuous ? "Running continuous ping..." : $"Running {pingCount} pings...";
            
            while ((isContinuous || count < pingCount) && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    PacketsSent++;
                    var reply = await _pingSender.SendPingAsync(target, 5000); // 5 second timeout
                    
                    var pingResult = new PingResult
                    {
                        PingNumber = count + 1,
                        Timestamp = DateTime.Now
                    };
                    
                    if (reply.Status == IPStatus.Success)
                    {
                        PacketsReceived++;
                        responseTimes.Add(reply.RoundtripTime);
                        totalTime += reply.RoundtripTime;
                        minTime = Math.Min(minTime, reply.RoundtripTime);
                        maxTime = Math.Max(maxTime, reply.RoundtripTime);
                        
                        pingResult.Status = "Success";
                        pingResult.StatusColor = Color.FromArgb("#4CAF50");
                        pingResult.Time = $"{reply.RoundtripTime} ms";
                        
                        // Update response time statistics
                        MinResponseTime = minTime;
                        MaxResponseTime = maxTime;
                        AvgResponseTime = (double)totalTime / PacketsReceived;
                        HasResponseTimes = true;
                    }
                    else
                    {
                        PacketsLost++;
                        pingResult.Status = $"Failed ({reply.Status})";
                        pingResult.StatusColor = Color.FromArgb("#F44336");
                        pingResult.Time = "N/A";
                    }
                    
                    // Add to results (keep only last 50 for performance)
                    if (PingResults.Count >= 50)
                    {
                        PingResults.RemoveAt(0);
                    }
                    PingResults.Add(pingResult);
                    
                    // Update loss percentage
                    LossPercentage = PacketsSent > 0 ? (double)PacketsLost / PacketsSent * 100 : 0;
                    
                    // Update status message
                    if (!isContinuous)
                    {
                        StatusMessage = $"Progress: {count + 1}/{pingCount} pings";
                    }
                    else
                    {
                        StatusMessage = $"Sent: {PacketsSent}, Received: {PacketsReceived}";
                    }
                }
                catch (Exception ex)
                {
                    PacketsLost++;
                    
                    var pingResult = new PingResult
                    {
                        PingNumber = count + 1,
                        Status = "Error",
                        StatusColor = Color.FromArgb("#FF5722"),
                        Time = "Error",
                        Timestamp = DateTime.Now
                    };
                    
                    if (PingResults.Count >= 50)
                    {
                        PingResults.RemoveAt(0);
                    }
                    PingResults.Add(pingResult);
                    
                    LossPercentage = PacketsSent > 0 ? (double)PacketsLost / PacketsSent * 100 : 0;
                    
                    _logger.LogWarning(ex, "Ping failed for {Target}", target);
                }
                
                count++;
                
                // Wait 1 second between pings
                try
                {
                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            
            // Final status update
            if (!cancellationToken.IsCancellationRequested)
            {
                Status = "Completed";
                StatusMessage = $"Test completed. Sent: {PacketsSent}, Received: {PacketsReceived}, Lost: {PacketsLost}";
            }
        }
        
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _pingSender?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
