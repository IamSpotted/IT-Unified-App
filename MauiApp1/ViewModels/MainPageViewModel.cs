using System.Collections.ObjectModel;

namespace MauiApp1.ViewModels;

/// <summary>
/// Dashboard Alert model for displaying SolarWinds alerts
/// </summary>
public class DashboardAlert
{
    public string StatusIcon { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string AlertMessage { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
}

/// <summary>
/// Critical Device model for devices needing attention
/// </summary>
public class CriticalDevice
{
    public string StatusIcon { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string OfflineTime { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Color PriorityColor { get; set; } = Colors.Red;
}

/// <summary>
/// MainPage ViewModel following MVVM and SOLID principles - Now serving as Dashboard
/// </summary>
public partial class MainPageViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;

    // Dashboard Status Properties
    [ObservableProperty]
    private string _onlineDevices = "2,847";

    [ObservableProperty]
    private string _offlineDevices = "23";

    [ObservableProperty]
    private string _warningDevices = "18";

    [ObservableProperty]
    private string _totalDevices = "2,888";

    [ObservableProperty]
    private string _onlinePercentage = "98.6%";

    [ObservableProperty]
    private string _offlinePercentage = "0.8%";

    [ObservableProperty]
    private string _warningPercentage = "0.6%";

    [ObservableProperty]
    private string _lastRefreshTime = "2 minutes ago";

    [ObservableProperty]
    private string _lastSolarWindsSync = "1 minute ago";

    [ObservableProperty]
    private string _monitoringStatus = "Active";

    [ObservableProperty]
    private string _pollingInterval = "5 minutes";

    // Collections
    [ObservableProperty]
    private ObservableCollection<DashboardAlert> _recentAlerts = new();

    [ObservableProperty]
    private ObservableCollection<CriticalDevice> _criticalDevices = new();

    public MainPageViewModel(
        ILogger<MainPageViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        ISettingsService settingsService) 
        : base(logger, navigationService, dialogService)
    {
        Title = "Dashboard";
        _settingsService = settingsService;
        
        // Initialize dashboard data
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            await LoadMockDashboardData();
        }, "Initialize Dashboard");
    }

    private async Task LoadMockDashboardData()
    {
        // Simulate loading delay
        await Task.Delay(500);

        // Load mock recent alerts
        RecentAlerts.Clear();
        RecentAlerts.Add(new DashboardAlert 
        { 
            StatusIcon = "🔴", 
            DeviceName = "Core-Switch-01", 
            AlertMessage = "Node Down - No response", 
            TimeAgo = "5 min ago" 
        });
        RecentAlerts.Add(new DashboardAlert 
        { 
            StatusIcon = "🟡", 
            DeviceName = "Printer-West-105", 
            AlertMessage = "High response time (2.5s)", 
            TimeAgo = "12 min ago" 
        });
        RecentAlerts.Add(new DashboardAlert 
        { 
            StatusIcon = "🟢", 
            DeviceName = "Camera-East-220", 
            AlertMessage = "Connection restored", 
            TimeAgo = "18 min ago" 
        });
        RecentAlerts.Add(new DashboardAlert 
        { 
            StatusIcon = "🔴", 
            DeviceName = "Server-DB-Main", 
            AlertMessage = "CPU usage critical (95%)", 
            TimeAgo = "25 min ago" 
        });
        RecentAlerts.Add(new DashboardAlert 
        { 
            StatusIcon = "🟡", 
            DeviceName = "Firewall-DMZ-02", 
            AlertMessage = "Memory usage high (88%)", 
            TimeAgo = "32 min ago" 
        });

        // Load mock critical devices
        CriticalDevices.Clear();
        CriticalDevices.Add(new CriticalDevice 
        { 
            StatusIcon = "🔴", 
            Name = "Core-Switch-01", 
            Location = "Server Room A-1", 
            OfflineTime = "2h 15m", 
            Priority = "CRITICAL",
            PriorityColor = Colors.Red
        });
        CriticalDevices.Add(new CriticalDevice 
        { 
            StatusIcon = "🔴", 
            Name = "Printer-Hub-Main", 
            Location = "Office-North-Floor3", 
            OfflineTime = "1h 43m", 
            Priority = "HIGH",
            PriorityColor = Colors.Orange
        });
        CriticalDevices.Add(new CriticalDevice 
        { 
            StatusIcon = "🔴", 
            Name = "Camera-Security-12", 
            Location = "Warehouse-East-B2", 
            OfflineTime = "45m", 
            Priority = "MEDIUM",
            PriorityColor = Colors.Gold
        });
        CriticalDevices.Add(new CriticalDevice 
        { 
            StatusIcon = "🟡", 
            Name = "Sensor-Temp-West", 
            Location = "Production-West-A4", 
            OfflineTime = "23m", 
            Priority = "LOW",
            PriorityColor = Colors.Gray
        });

        // Update timestamps
        LastRefreshTime = "Just now";
        LastSolarWindsSync = "1 minute ago";
    }

    [RelayCommand]
    private async Task ViewAllAlerts()
    {
        await ExecuteSafelyAsync(async () =>
        {
            await _dialogService.ShowAlertAsync("SolarWinds Integration", 
                "This would open the SolarWinds alerts dashboard.\n\n" +
                "Future implementation:\n" +
                "• Direct link to SolarWinds web interface\n" +
                "• API integration for alerts data\n" +
                "• Real-time alert notifications");
        }, "View All Alerts");
    }

    [RelayCommand]
    private async Task RefreshStatus()
    {
        await ExecuteSafelyAsync(async () =>
        {
            IsBusy = true;
            
            // Simulate refresh operation
            await Task.Delay(2000);
            
            // Reload dashboard data
            await LoadMockDashboardData();
            
            await _dialogService.ShowAlertAsync("Refresh Complete", 
                "Dashboard data has been refreshed with latest information from SolarWinds.");
                
        }, "Refresh Status");
    }

    [RelayCommand]
    private async Task OpenSolarWinds()
    {
        await ExecuteSafelyAsync(async () =>
        {
            await _dialogService.ShowAlertAsync("SolarWinds Dashboard", 
                "This would open the SolarWinds web interface.\n\n" +
                "Implementation options:\n" +
                "• Launch default browser to SolarWinds URL\n" +
                "• Open embedded web view\n" +
                "• Deep link to specific SolarWinds dashboards");
        }, "Open SolarWinds");
    }
}
