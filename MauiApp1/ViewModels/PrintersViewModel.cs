namespace MauiApp1.ViewModels;

public partial class PrintersViewModel : FilterableBaseViewModel<Printer>, ILoadableViewModel, IDisposable
{
    private readonly IPrinterService _printerService;
    private readonly ISettingsService _settingsService;
    private readonly SecureCredentialsService _credentialsService;
    private bool _disposed = false;

    [ObservableProperty]
    private Printer? _selectedPrinter;

    [ObservableProperty]
    private int _gridSpan = 5;

    // Convenience properties for UI binding
    public ObservableCollection<Printer> Printers => Items;
    public ObservableCollection<string> AvailableAreas => FilterOptions.TryGetValue("Area", out var areas) ? areas : new();
    public ObservableCollection<string> AvailableZones => FilterOptions.TryGetValue("Zone", out var zones) ? zones : new();
    public ObservableCollection<string> AvailableLines => FilterOptions.TryGetValue("Line", out var lines) ? lines : new();
    public ObservableCollection<string> AvailableColumns => FilterOptions.TryGetValue("Column", out var columns) ? columns : new();
    public ObservableCollection<string> AvailableLevels => FilterOptions.TryGetValue("Level", out var levels) ? levels : new();
    public ObservableCollection<string> AvailableModels => FilterOptions.TryGetValue("Model", out var models) ? models : new();

    public string SelectedArea
    {
        get => SelectedFilters.TryGetValue("Area", out var area) ? area : "";
        set => OnFilterChanged("Area", value);
    }

    public string SelectedZone
    {
        get => SelectedFilters.TryGetValue("Zone", out var zone) ? zone : "";
        set => OnFilterChanged("Zone", value);
    }

    public string SelectedLine
    {
        get => SelectedFilters.TryGetValue("Line", out var line) ? line : "";
        set => OnFilterChanged("Line", value);
    }

    public string SelectedColumn
    {
        get => SelectedFilters.TryGetValue("Column", out var column) ? column : "";
        set => OnFilterChanged("Column", value);
    }

    public string SelectedLevel
    {
        get => SelectedFilters.TryGetValue("Level", out var level) ? level : "";
        set => OnFilterChanged("Level", value);
    }

    public string SelectedModel
    {
        get => SelectedFilters.TryGetValue("Model", out var model) ? model : "";
        set => OnFilterChanged("Model", value);
    }

    public PrintersViewModel(
        ILogger<PrintersViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        IPrinterService printerService,
        IFilterService<Printer> filterService,
        ISettingsService settingsService,
        SecureCredentialsService credentialsService) 
        : base(logger, navigationService, dialogService, filterService)
    {
        Title = "Printers";
        _printerService = printerService;
        _settingsService = settingsService;
        _credentialsService = credentialsService;
        
        // Load grid span from settings
        _ = LoadGridSpanAsync();
        
        // Listen for grid span changes from settings
        SettingsManager.GridSpanChanged += OnGridSpanChanged;
        
        // Initialize printers
        _ = LoadPrintersAsync();
    }

    protected override string[] GetFilterProperties()
    {
        return new[] { "Area", "Zone", "Line", "Column", "Level", "Model" };
    }
    public async Task<string> GetPrinterDetailsAsync(Printer printer)
    {
        _logger.LogInformation("Getting details for printer: {PrinterName}", printer.Hostname);
        await Task.Delay(100); // Simulate network delay
        
        return $"Printer: {printer.Hostname}\n" +
               $"IP Address: {printer.PrimaryIp}\n" +
               $"Model: {printer.Model}\n" +
               $"Serial: {printer.SerialNumber}\n" +
               $"Location: {printer.FullLocation}\n" +
               $"Web Interface: {printer.WebInterfaceUrl}";
    }

    [RelayCommand]
    private async Task LoadPrinters()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var printers = await _printerService.GetPrintersAsync();
            LoadItems(printers);
            _logger.LogInformation("Loaded {Count} printers", printers.Count());
        }, "Load Printers");
    }

    [RelayCommand]
    private async Task SelectPrinter(Printer printer)
    {
        await ExecuteSafelyAsync(async () =>
        {
            if (printer == null) return;
            SelectedPrinter = printer;

            if (Application.Current?.MainPage == null) return;

            var details = $"Printer: {printer.Hostname}\n" +
                          $"IP Address: {printer.PrimaryIp}\n" +
                          $"Model: {printer.Model}\n" +
                          $"Serial: {printer.SerialNumber}\n" +
                          $"Location: {printer.FullLocation}\n" +
                          $"Web Interface: {printer.WebInterfaceUrl}";

            var action = await Application.Current.MainPage.DisplayActionSheet(
                $" {printer.Hostname}",
                "Cancel",
                null,
                " Open Web Interface",
                " Show Details");

            switch (action)
            {
                case "Open Web Interface":
                    await OpenWebInterface(printer);
                    break;
                case "Show Details":
                    var status = await GetSolarWindsStatusAsync(printer);
                    var statusEmoji = status?.IsOnline == true ? "🟢" : "🔴";
                    var statusText = status?.IsOnline == true ? "Online" : "Offline";
                    var responseTime = status?.ResponseTimeMs > 0 ? $"{status.ResponseTimeMs} ms" : "";

                    var enhancedDetails = $"{details}\n\nLive Status: {statusText}\nResponse Time: {status?.ResponseTimeMs ?? 0}ms\nLast Checked: {DateTime.Now:HH:mm:ss}";
                    await Application.Current.MainPage.DisplayAlert(
                        $"Printer Details {statusEmoji} {statusText}{responseTime}", enhancedDetails, "OK");
                    break;
            }
        }, "Select Printer");
    }

    [RelayCommand]
    private async Task OpenWebInterface(Printer printer)
    {
        try
        {
            _logger.LogInformation("Opening web interface for printer {Hostname} at {Url}", 
                printer.Hostname, printer.WebInterfaceUrl);

            // Check if URL is valid
            if (string.IsNullOrWhiteSpace(printer.WebInterfaceUrl))
            {
                await _dialogService.ShowAlertAsync("Error", "Web interface URL is empty or null");
                return;
            }

            if (!Uri.TryCreate(printer.WebInterfaceUrl, UriKind.Absolute, out var uri))
            {
                await _dialogService.ShowAlertAsync("Error", $"Invalid URL format: {printer.WebInterfaceUrl}");
                return;
            }

            _logger.LogInformation("Attempting to launch URL using Process.Start: {Url}", printer.WebInterfaceUrl);
            
            // Use the same pattern as the working console app
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(printer.WebInterfaceUrl) { UseShellExecute = true });

            _logger.LogInformation("Successfully opened web interface for printer {Hostname}", printer.Hostname);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open web interface for printer {Hostname}. URL: {Url}", 
                printer.Hostname, printer.WebInterfaceUrl);
            await _dialogService.ShowAlertAsync("Error", 
                $"Unable to open web interface for {printer.Hostname}.\nURL: {printer.WebInterfaceUrl}\nError: {ex.Message}");
        }
    }

    // Method to query SolarWinds for device status
    private async Task<DeviceStatus?> GetSolarWindsStatusAsync(Printer printer)
    {
        try
        {
            // Get stored credentials
            var credentials = await _credentialsService.GetSolarWindsCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogWarning("No SolarWinds credentials configured for status query");
                return new DeviceStatus
                {
                    IsOnline = false,
                    ResponseTimeMs = 0,
                    StatusDescription = "No Credentials",
                    LastChecked = DateTime.Now
                };
            }

            // Execute PowerShell to query SolarWinds SWIS
            var powerShellScript = $@"
                try {{
                    # Import SolarWinds SWIS module
                    Import-Module SwisPowerShell -ErrorAction Stop
                    
                    # Connect to SolarWinds
                    $swis = Connect-Swis -Hostname '{credentials.Hostname}' -Username '{credentials.Username}' -Password '{credentials.Password}' -ErrorAction Stop
                    
                    # Query for device status - try multiple possible identifiers
                    $queries = @(
                        ""SELECT NodeID, Status, StatusDescription, Caption, IP_Address FROM Orion.Nodes WHERE Caption = '{printer.Hostname}'"",
                        ""SELECT NodeID, Status, StatusDescription, Caption, IP_Address FROM Orion.Nodes WHERE IP_Address = '{printer.PrimaryIp}'"",
                        ""SELECT NodeID, Status, StatusDescription, Caption, IP_Address FROM Orion.Nodes WHERE Caption LIKE '%{printer.Hostname}%'""
                    )
                    
                    $result = $null
                    foreach ($query in $queries) {{
                        $result = Get-SwisData $swis $query -ErrorAction SilentlyContinue
                        if ($result) {{ break }}
                    }}
                    
                    if ($result) {{
                        # SolarWinds Status: 1=Up, 2=Down, 3=Warning, etc.
                        $isOnline = $result.Status -eq 1
                        $statusDesc = if ($result.StatusDescription) {{ $result.StatusDescription }} else {{ if ($isOnline) {{ ""Up"" }} else {{ ""Down"" }} }}
                        
                        Write-Output ""SUCCESS|$isOnline|$statusDesc|$($result.Caption)|$($result.IP_Address)""
                    }} else {{
                        Write-Output ""NOT_FOUND|False|Device not found in SolarWinds||""
                    }}
                }} catch {{
                    Write-Output ""ERROR|False|$($_.Exception.Message)||""
                }}
            ";

            var result = await ExecutePowerShellAsync(powerShellScript);
            
            // Parse PowerShell result
            var parts = result.Split('|');
            if (parts.Length >= 3)
            {
                var status = parts[0];
                var isOnlineStr = parts[1];
                var statusDescription = parts[2];
                var foundCaption = parts.Length > 3 ? parts[3] : "";
                var foundIpAddress = parts.Length > 4 ? parts[4] : "";

                var isOnline = bool.Parse(isOnlineStr);

                _logger.LogInformation("SolarWinds query result for {Hostname}: Status={Status}, Online={IsOnline}, Description={Description}", 
                    printer.Hostname, status, isOnline, statusDescription);

                return new DeviceStatus
                {
                    IsOnline = isOnline,
                    ResponseTimeMs = 0, // Not needed anymore
                    StatusDescription = statusDescription,
                    LastChecked = DateTime.Now
                };
            }
            else
            {
                _logger.LogWarning("Invalid PowerShell result format: {Result}", result);
                return new DeviceStatus
                {
                    IsOnline = false,
                    ResponseTimeMs = 0,
                    StatusDescription = "Parse Error",
                    LastChecked = DateTime.Now
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query SolarWinds status for {Hostname}", printer.Hostname);
            return new DeviceStatus
            {
                IsOnline = false,
                ResponseTimeMs = 0,
                StatusDescription = "Error",
                LastChecked = DateTime.Now
            };
        }
    }

    // Helper method to execute PowerShell scripts
    private async Task<string> ExecutePowerShellAsync(string script)
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();

            if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
            {
                _logger.LogError("PowerShell execution failed. Exit code: {ExitCode}, Error: {Error}", process.ExitCode, error);
                return $"ERROR|False|PowerShell execution failed: {error}||";
            }

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute PowerShell script");
            return $"ERROR|False|Exception: {ex.Message}||";
        }
    }

    private async Task LoadPrintersAsync()
    {
        await LoadPrinters();
    }

    protected override IEnumerable<Printer> ApplySearchFilter(IEnumerable<Printer> items, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return items;

        var lowerSearchText = searchText.ToLowerInvariant();
        return items.Where(printer =>
            (printer.Hostname ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (printer.Model ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (printer.Area ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (printer.Zone ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (printer.Line ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (printer.Column ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (printer.Level ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (printer.PrimaryIp ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (printer.SerialNumber ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase) ||
            (printer.FullLocation ?? string.Empty).Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)
        );
    }

    private async Task LoadGridSpanAsync()
    {
        try
        {
            var gridSpanSetting = await _settingsService.GetAsync<int?>("grid_span", 5);
            GridSpan = gridSpanSetting ?? 5;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load grid span setting");
            GridSpan = 5; // Default fallback
        }
    }

    private void OnGridSpanChanged(object? sender, int newGridSpan)
    {
        GridSpan = newGridSpan;
        _logger.LogInformation("Grid span updated to: {GridSpan}", newGridSpan);
    }

    private void OnPrintersChanged(object? sender, EventArgs e)
    {
        // Reload printers when the service notifies of changes
        _logger.LogInformation("Printers changed - reloading data");
        _ = LoadPrintersAsync();
    }

    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Unsubscribe from static events to prevent memory leaks
            SettingsManager.GridSpanChanged -= OnGridSpanChanged;
            _disposed = true;
        }
    }

    /// <summary>
    /// Implementation of ILoadableViewModel.LoadDataCommand
    /// Aliases to LoadPrintersCommand for automatic page loading
    /// </summary>
    public IAsyncRelayCommand LoadDataCommand => LoadPrintersCommand;
}
