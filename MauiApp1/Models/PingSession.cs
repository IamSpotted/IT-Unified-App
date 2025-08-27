using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiApp1.Models;

public class PingSession : INotifyPropertyChanged
{
    private string _id;
    private string _target;
    private PingSessionStatus _status;
    private int _packetsSent;
    private int _packetsReceived;
    private int _packetsLost;
    private double _lossPercentage;
    private long _minResponseTime;
    private long _maxResponseTime;
    private double _avgResponseTime;
    private bool _hasResponseTimes;
    private string _statusMessage;
    private DateTime _startTime;
    private DateTime? _endTime;
    private CancellationTokenSource? _cancellationTokenSource;

    public PingSession(string target, int pingCount = 10)
    {
        _id = Guid.NewGuid().ToString();
        _target = target;
        PingCount = pingCount;
        _status = PingSessionStatus.Ready;
        _statusMessage = pingCount == -1 ? "Ready to start (Continuous)" : "Ready to start";
        Results = new ObservableCollection<PingResult>();
        _startTime = DateTime.Now;
    }

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Target
    {
        get => _target;
        set => SetProperty(ref _target, value);
    }

    public int PingCount { get; set; }

    public PingSessionStatus Status
    {
        get => _status;
        set 
        {
            if (SetProperty(ref _status, value))
            {
                // Also notify that IsRunning might have changed
                OnPropertyChanged(nameof(IsRunning));
            }
        }
    }

    public int PacketsSent
    {
        get => _packetsSent;
        set => SetProperty(ref _packetsSent, value);
    }

    public int PacketsReceived
    {
        get => _packetsReceived;
        set
        {
            SetProperty(ref _packetsReceived, value);
            PacketsLost = PacketsSent - PacketsReceived;
            LossPercentage = PacketsSent > 0 ? (double)PacketsLost / PacketsSent * 100 : 0;
        }
    }

    public int PacketsLost
    {
        get => _packetsLost;
        private set => SetProperty(ref _packetsLost, value);
    }

    public double LossPercentage
    {
        get => _lossPercentage;
        private set => SetProperty(ref _lossPercentage, value);
    }

    public long MinResponseTime
    {
        get => _minResponseTime;
        set => SetProperty(ref _minResponseTime, value);
    }

    public long MaxResponseTime
    {
        get => _maxResponseTime;
        set => SetProperty(ref _maxResponseTime, value);
    }

    public double AvgResponseTime
    {
        get => _avgResponseTime;
        set => SetProperty(ref _avgResponseTime, value);
    }

    public bool HasResponseTimes
    {
        get => _hasResponseTimes;
        set => SetProperty(ref _hasResponseTimes, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public DateTime StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public DateTime? EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }

    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    public ObservableCollection<PingResult> Results { get; }

    public CancellationTokenSource? CancellationTokenSource
    {
        get => _cancellationTokenSource;
        set => SetProperty(ref _cancellationTokenSource, value);
    }

    public bool IsRunning => Status == PingSessionStatus.Running;
    public bool IsCompleted => Status == PingSessionStatus.Completed || Status == PingSessionStatus.Error;
    public bool IsContinuous => PingCount == -1;
    public string PingCountDisplay => IsContinuous ? "Continuous" : PingCount.ToString();

    public void UpdateStatistics()
    {
        var successfulPings = Results.Where(r => r.Status == "Success").ToList();
        
        if (successfulPings.Any())
        {
            var responseTimes = new List<long>();
            foreach (var ping in successfulPings)
            {
                // Parse the time string to extract milliseconds
                if (ping.Time.EndsWith(" ms") && long.TryParse(ping.Time.Replace(" ms", ""), out long time))
                {
                    responseTimes.Add(time);
                }
            }

            if (responseTimes.Any())
            {
                MinResponseTime = responseTimes.Min();
                MaxResponseTime = responseTimes.Max();
                AvgResponseTime = responseTimes.Average();
                HasResponseTimes = true;
            }
            else
            {
                HasResponseTimes = false;
            }
        }
        else
        {
            HasResponseTimes = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public enum PingSessionStatus
{
    Ready,
    Running,
    Completed,
    Stopped,
    Error
}
