namespace MauiApp1.Controls;

public partial class SevenSegmentClock : ContentView, IDisposable
{
    private System.Timers.Timer? _timer;
    private bool _disposed;

    public SevenSegmentClock()
    {
        InitializeComponent();
        StartClock();
    }

    private void StartClock()
    {
        UpdateTime();
        
        _timer = new System.Timers.Timer(1000); // Update every second
        _timer.Elapsed += (s, e) => UpdateTime();
        _timer.Start();
    }

    private void UpdateTime()
    {
        var now = DateTime.Now;
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update date (MM/DD/YYYY)
            var dateStr = now.ToString("MMddyyyy");
            DateMonth1.Digit = dateStr[0];
            DateMonth2.Digit = dateStr[1];
            DateDay1.Digit = dateStr[2];
            DateDay2.Digit = dateStr[3];
            DateYear1.Digit = dateStr[4];
            DateYear2.Digit = dateStr[5];
            DateYear3.Digit = dateStr[6];
            DateYear4.Digit = dateStr[7];
            
            // Update time (HH:MM:SS)
            var timeStr = now.ToString("HHmmss");
            TimeHour1.Digit = timeStr[0];
            TimeHour2.Digit = timeStr[1];
            TimeMinute1.Digit = timeStr[2];
            TimeMinute2.Digit = timeStr[3];
            TimeSecond1.Digit = timeStr[4];
            TimeSecond2.Digit = timeStr[5];
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}
