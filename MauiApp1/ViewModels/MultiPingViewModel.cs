using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;

namespace MauiApp1.ViewModels;

public partial class MultiPingViewModel : ObservableObject
{
    private readonly Dictionary<string, Ping> _pingInstances = new();

    [ObservableProperty]
    private string _newTarget = string.Empty;

    [ObservableProperty]
    private string _defaultPingCount = "10";

    [ObservableProperty]
    private bool _hasActiveSessions;

    [ObservableProperty]
    private int _runningSessions;

    [ObservableProperty]
    private int _completedSessions;

    public ObservableCollection<PingSession> PingSessions { get; } = new();

    public MultiPingViewModel()
    {
        // Monitor sessions for status changes
        PingSessions.CollectionChanged += (s, e) => UpdateSessionCounts();
    }

    [RelayCommand]
    private void AddPingSession()
    {
        if (string.IsNullOrWhiteSpace(NewTarget))
            return;

        // Parse ping count - handle "Continuous" as -1
        int pingCount;
        if (DefaultPingCount.Equals("Continuous", StringComparison.OrdinalIgnoreCase))
        {
            pingCount = -1; // -1 indicates continuous pinging
        }
        else if (!int.TryParse(DefaultPingCount, out pingCount))
        {
            pingCount = 10; // Default fallback
        }

        var session = new PingSession(NewTarget.Trim(), pingCount);
        session.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(PingSession.Status))
                UpdateSessionCounts();
        };

        PingSessions.Add(session);
        NewTarget = string.Empty;
        UpdateSessionCounts();
    }

    [RelayCommand]
    private void StartSession(PingSession session)
    {
        if (session.Status == PingSessionStatus.Running)
            return;

        session.Status = PingSessionStatus.Running;
        session.StartTime = DateTime.Now;
        session.EndTime = null;
        session.StatusMessage = "Starting ping test...";
        session.CancellationTokenSource = new CancellationTokenSource();

        // Clear previous results if restarting
        session.Results.Clear();
        session.PacketsSent = 0;
        session.PacketsReceived = 0;

        // Start the ping operation in the background to not block other sessions
        _ = Task.Run(async () => await RunPingSession(session));
    }

    private async Task RunPingSession(PingSession session)
    {
        var ping = new Ping();
        _pingInstances[session.Id] = ping;

        try
        {
            if (session.IsContinuous)
            {
                // Continuous pinging - run until stopped
                int pingNumber = 1;
                while (session.CancellationTokenSource?.Token.IsCancellationRequested == false)
                {
                    session.StatusMessage = $"Pinging {session.Target} continuously (#{pingNumber})...";
                    session.PacketsSent = pingNumber;

                    try
                    {
                        var reply = await ping.SendPingAsync(session.Target, 5000);
                        var result = new PingResult
                        {
                            PingNumber = pingNumber,
                            Timestamp = DateTime.Now
                        };

                        if (reply.Status == IPStatus.Success)
                        {
                            session.PacketsReceived++;
                            result.Status = "Success";
                            result.StatusColor = Color.FromArgb("#4CAF50");
                            result.Time = $"{reply.RoundtripTime} ms";
                        }
                        else
                        {
                            result.Status = $"Failed ({reply.Status})";
                            result.StatusColor = Color.FromArgb("#F44336");
                            result.Time = "N/A";
                        }

                        session.Results.Add(result);
                        
                        // Keep only last 100 results for performance in continuous mode
                        if (session.Results.Count > 100)
                        {
                            session.Results.RemoveAt(0);
                        }
                        
                        session.UpdateStatistics();
                    }
                    catch (Exception ex)
                    {
                        var result = new PingResult
                        {
                            PingNumber = pingNumber,
                            Status = $"Error: {ex.Message}",
                            StatusColor = Color.FromArgb("#F44336"),
                            Time = "N/A",
                            Timestamp = DateTime.Now
                        };

                        session.Results.Add(result);
                    }

                    pingNumber++;
                    
                    // Wait between pings unless cancelled
                    if (session.CancellationTokenSource?.Token.IsCancellationRequested == false)
                    {
                        try
                        {
                            await Task.Delay(1000, session.CancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            // Cancellation requested during delay, break out of loop
                            break;
                        }
                    }
                }

                if (session.Status == PingSessionStatus.Running)
                {
                    session.Status = PingSessionStatus.Stopped;
                    session.StatusMessage = $"Continuous ping stopped after {pingNumber - 1} packets";
                }
            }
            else
            {
                // Fixed count pinging
                for (int i = 1; i <= session.PingCount; i++)
                {
                    if (session.CancellationTokenSource?.Token.IsCancellationRequested == true)
                    {
                        session.Status = PingSessionStatus.Stopped;
                        session.StatusMessage = "Ping test stopped";
                        break;
                    }

                    session.StatusMessage = $"Pinging {session.Target} ({i}/{session.PingCount})...";
                    session.PacketsSent = i;

                    try
                    {
                        var reply = await ping.SendPingAsync(session.Target, 5000);
                        var result = new PingResult
                        {
                            PingNumber = i,
                            Timestamp = DateTime.Now
                        };

                        if (reply.Status == IPStatus.Success)
                        {
                            session.PacketsReceived++;
                            result.Status = "Success";
                            result.StatusColor = Color.FromArgb("#4CAF50");
                            result.Time = $"{reply.RoundtripTime} ms";
                        }
                        else
                        {
                            result.Status = $"Failed ({reply.Status})";
                            result.StatusColor = Color.FromArgb("#F44336");
                            result.Time = "N/A";
                        }

                        session.Results.Add(result);
                        session.UpdateStatistics();
                    }
                    catch (Exception ex)
                    {
                        var result = new PingResult
                        {
                            PingNumber = i,
                            Status = $"Error: {ex.Message}",
                            StatusColor = Color.FromArgb("#F44336"),
                            Time = "N/A",
                            Timestamp = DateTime.Now
                        };

                        session.Results.Add(result);
                    }

                    // Wait between pings unless this is the last one
                    if (i < session.PingCount && session.CancellationTokenSource?.Token.IsCancellationRequested == false)
                    {
                        try
                        {
                            await Task.Delay(1000, session.CancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            // Cancellation requested during delay, break out of loop
                            break;
                        }
                    }
                }

                if (session.Status == PingSessionStatus.Running)
                {
                    session.Status = PingSessionStatus.Completed;
                    session.StatusMessage = "Ping test completed";
                }
            }
        }
        catch (OperationCanceledException)
        {
            session.Status = PingSessionStatus.Stopped;
            session.StatusMessage = "Ping test stopped";
        }
        catch (Exception ex)
        {
            session.Status = PingSessionStatus.Error;
            session.StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            session.EndTime = DateTime.Now;
            ping.Dispose();
            _pingInstances.Remove(session.Id);
        }
    }

    [RelayCommand]
    private void StopSession(PingSession session)
    {
        System.Diagnostics.Debug.WriteLine($"StopSession called for {session.Target}, current status: {session.Status}");
        
        if (session.Status != PingSessionStatus.Running)
        {
            System.Diagnostics.Debug.WriteLine($"Cannot stop session {session.Target} - not in running state");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Cancelling session {session.Target}");
        session.CancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    private void RemoveSession(PingSession session)
    {
        if (session.Status == PingSessionStatus.Running)
        {
            StopSession(session);
        }

        PingSessions.Remove(session);
    }

    [RelayCommand]
    private void StartAllSessions()
    {
        var readySessions = PingSessions.Where(s => s.Status == PingSessionStatus.Ready || s.Status == PingSessionStatus.Stopped || s.Status == PingSessionStatus.Completed).ToList();
        
        foreach (var session in readySessions)
        {
            StartSession(session);
        }
    }

    [RelayCommand]
    private void StopAllSessions()
    {
        var runningSessions = PingSessions.Where(s => s.Status == PingSessionStatus.Running).ToList();
        
        foreach (var session in runningSessions)
        {
            StopSession(session);
        }
    }

    [RelayCommand]
    private void ClearCompletedSessions()
    {
        var completedSessions = PingSessions.Where(s => s.IsCompleted).ToList();
        
        foreach (var session in completedSessions)
        {
            PingSessions.Remove(session);
        }
    }

    [RelayCommand]
    private async Task ViewSessionDetails(PingSession session)
    {
        // Navigate to detailed view with session ID parameter
        await Shell.Current.GoToAsync($"{nameof(PingSessionDetailPage)}?sessionId={session.Id}");
    }

    private void UpdateSessionCounts()
    {
        RunningSessions = PingSessions.Count(s => s.Status == PingSessionStatus.Running);
        CompletedSessions = PingSessions.Count(s => s.IsCompleted);
        HasActiveSessions = PingSessions.Any();
    }
}
