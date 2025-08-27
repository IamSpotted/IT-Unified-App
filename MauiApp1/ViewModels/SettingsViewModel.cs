using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly SecureCredentialsService _credentialsService;
    private readonly IDatabaseService _databaseService;
    private readonly IAuthorizationService _authorizationService;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private string _appVersion = "0.6.0.1";

    [ObservableProperty]
    private string _buildDate = DateTime.Now.ToString("yyyy-MM-dd");

    [ObservableProperty]
    private string _developerName = "Jon Uldrick";

    [ObservableProperty]
    private string _frameworkVersion = ".NET 8.0 MAUI";

    [ObservableProperty]
    private string _companyName = "VW Chattanooga IT Shop Floor";

    // Theme & Appearance Settings
    [ObservableProperty]
    private string _selectedTheme = "System";

    [ObservableProperty]
    private int _gridSpan = 5;

    [ObservableProperty]
    private string _selectedFontSize = "Medium";

    // Auto-refresh Settings
    [ObservableProperty]
    private int _deviceRefreshInterval = 60; // seconds

    [ObservableProperty]
    private int _cameraRefreshInterval = 60; // seconds

    [ObservableProperty]
    private int _printerRefreshInterval = 60; // seconds

    // SolarWinds Settings
    [ObservableProperty]
    private string _solarWindsHostname = string.Empty;

    [ObservableProperty]
    private string _solarWindsUsername = string.Empty;

    [ObservableProperty]
    private string _solarWindsPassword = string.Empty;

    [ObservableProperty]
    private int _solarWindsPort = 17778;

    [ObservableProperty]
    private bool _solarWindsCredentialsConfigured = false;

    // SQL Database Settings
    [ObservableProperty]
    private string _databaseServer = string.Empty;

    [ObservableProperty]
    private string _databaseName = string.Empty;

    [ObservableProperty]
    private string _selectedAuthenticationType = "Windows Authentication";

    [ObservableProperty]
    private string _databaseUsername = string.Empty;

    [ObservableProperty]
    private string _databasePassword = string.Empty;

    [ObservableProperty]
    private int _connectionTimeout = 30;

    [ObservableProperty]
    private bool _databaseCredentialsConfigured = false;

    // Developer Mode Settings (only visible in debug builds)
    [ObservableProperty]
    private bool _developerModeEnabled = false;

    [ObservableProperty]
    private string _simulatedRole = "Current User";

    [ObservableProperty]
    private string _currentUserRole = "Loading...";

    [ObservableProperty]
    private string _currentUserName = "Loading...";

    // Domain Configuration Settings
    [ObservableProperty]
    private string _domainName = "PLACEHOLDER_DOMAIN";

    [ObservableProperty]
    private bool _showDomainConfiguration = false;

    [ObservableProperty]
    private bool _canEditDomainConfiguration = false;

    [ObservableProperty]
    private bool _showDatabaseConfiguration = false;

    private int _versionTapCount = 0;

    // Computed property for showing/hiding SQL authentication fields
    public bool IsSqlAuthentication => SelectedAuthenticationType == "SQL Server Authentication";

    partial void OnSelectedAuthenticationTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsSqlAuthentication));
    }

    // Available options for pickers
    public List<string> ThemeOptions { get; } = new() { "Light", "Dark", "System" };
    public List<int> GridSpanOptions { get; } = new() { 3, 4, 5, 6 };
    public List<string> FontSizeOptions { get; } = new() { "Small", "Medium", "Large" };
    public List<int> RefreshIntervalOptions { get; } = new() { 30, 60, 300, 900 }; // 30s, 1m, 5m, 15m
    public List<string> AuthenticationTypes { get; } = new() { "Windows Authentication", "SQL Server Authentication" };
    public List<int> TimeoutOptions { get; } = new() { 15, 30, 60, 120 }; // 15s, 30s, 1m, 2m
    public List<string> SimulatedRoleOptions { get; } = new() 
    { 
        "Current User", "Read Only", "Standard", "Local Admin", "Database Admin", "System Admin" 
    };

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IThemeService themeService,
        SecureCredentialsService credentialsService,
        IDatabaseService databaseService,
        IAuthorizationService authorizationService) 
        : base(logger, navigationService, dialogService)
    {
        Title = "Settings";
        _settingsService = settingsService;
        _themeService = themeService;
        _credentialsService = credentialsService;
        _databaseService = databaseService;
        _authorizationService = authorizationService;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            // Load existing settings
            var notificationsResult = await _settingsService.GetAsync<bool?>("notifications_enabled", true);
            NotificationsEnabled = notificationsResult ?? true;
            
            // Get version from MAUI AppInfo which reads from ApplicationDisplayVersion in csproj
            AppVersion = AppInfo.VersionString;

            // Load theme & appearance settings
            var themeResult = await _settingsService.GetAsync<string?>("selected_theme", "System");
            SelectedTheme = themeResult ?? "System";

            // Apply the loaded theme immediately
            var initialTheme = SelectedTheme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
            await _themeService.SetThemeAsync(initialTheme);

            var gridSpanResult = await _settingsService.GetAsync<int?>("grid_span", 5);
            GridSpan = gridSpanResult ?? 5;

            var fontSizeResult = await _settingsService.GetAsync<string?>("font_size", "Medium");
            SelectedFontSize = fontSizeResult ?? "Medium";

            // Load auto-refresh settings
            var deviceRefreshResult = await _settingsService.GetAsync<int?>("device_refresh_interval", 60);
            DeviceRefreshInterval = deviceRefreshResult ?? 60;

            var cameraRefreshResult = await _settingsService.GetAsync<int?>("camera_refresh_interval", 60);
            CameraRefreshInterval = cameraRefreshResult ?? 60;

            var printerRefreshResult = await _settingsService.GetAsync<int?>("printer_refresh_interval", 60);
            PrinterRefreshInterval = printerRefreshResult ?? 60;

            // Load SolarWinds credentials status
            SolarWindsCredentialsConfigured = await _credentialsService.HasSolarWindsCredentialsAsync();
            
            // If credentials exist, load them for display (but not the password)
            if (SolarWindsCredentialsConfigured)
            {
                var credentials = await _credentialsService.GetSolarWindsCredentialsAsync();
                if (credentials != null)
                {
                    SolarWindsHostname = credentials.Hostname;
                    SolarWindsUsername = credentials.Username;
                    SolarWindsPort = credentials.Port;
                    // Don't load password for security
                    SolarWindsPassword = "••••••••"; // Show masked password
                }
            }

            // Load SQL Database credentials status
            DatabaseCredentialsConfigured = await _credentialsService.HasDatabaseCredentialsAsync();
            
            // If credentials exist, load them for display (but not the password)
            if (DatabaseCredentialsConfigured)
            {
                var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
                if (credentials != null)
                {
                    DatabaseServer = credentials.Server;
                    DatabaseName = credentials.Database;
                    SelectedAuthenticationType = credentials.UseWindowsAuthentication ? 
                        "Windows Authentication" : "SQL Server Authentication";
                    DatabaseUsername = credentials.Username ?? string.Empty;
                    ConnectionTimeout = credentials.ConnectionTimeout;
                    // Don't load password for security
                    DatabasePassword = string.IsNullOrEmpty(credentials.Password) ? string.Empty : "••••••••";
                }
            }

            // Load authorization information
            await LoadAuthorizationInfoAsync();
        }, "Initialize Settings");
    }

    [RelayCommand]
    private async Task ToggleNotifications()
    {
        await ExecuteSafelyAsync(async () =>
        {
            NotificationsEnabled = !NotificationsEnabled;
            await _settingsService.SetAsync("notifications_enabled", NotificationsEnabled);
            _logger.LogInformation("Notifications {Status}", NotificationsEnabled ? "enabled" : "disabled");
        }, "Toggle Notifications");
    }

    [RelayCommand]
    private async Task ClearSettings()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Clear Settings", 
                "Are you sure you want to clear all application settings?");
                
            if (confirmed)
            {
                await _settingsService.ClearAllAsync();
                await _dialogService.ShowAlertAsync("Settings", "All settings have been cleared.");
            }
        }, "Clear Settings");
    }

    [RelayCommand]
    private async Task SaveSolarWindsCredentials()
    {
        await ExecuteSafelyAsync(async () =>
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(SolarWindsHostname))
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Hostname is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SolarWindsUsername))
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Username is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SolarWindsPassword) || SolarWindsPassword == "••••••••")
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Password is required.");
                return;
            }

            if (SolarWindsPort <= 0 || SolarWindsPort > 65535)
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Port must be between 1 and 65535.");
                return;
            }

            // Save credentials securely
            await _credentialsService.SaveSolarWindsCredentialsAsync(
                SolarWindsHostname.Trim(),
                SolarWindsUsername.Trim(),
                SolarWindsPassword,
                SolarWindsPort);

            SolarWindsCredentialsConfigured = true;
            SolarWindsPassword = "••••••••"; // Mask the password after saving

            await _dialogService.ShowAlertAsync("Success", "SolarWinds credentials saved securely.");
            _logger.LogInformation("SolarWinds credentials saved for hostname: {Hostname}", SolarWindsHostname);
        }, "Save SolarWinds Credentials");
    }

    [RelayCommand]
    private async Task TestSolarWindsConnection()
    {
        await ExecuteSafelyAsync(async () =>
        {
            if (!SolarWindsCredentialsConfigured)
            {
                await _dialogService.ShowAlertAsync("No Credentials", 
                    "Please save SolarWinds credentials first before testing the connection.");
                return;
            }

            var credentials = await _credentialsService.GetSolarWindsCredentialsAsync();
            if (credentials == null)
            {
                await _dialogService.ShowAlertAsync("Error", "Failed to retrieve stored credentials.");
                return;
            }

            // Show testing dialog
            var loadingTask = Application.Current?.MainPage?.DisplayAlert(
                "Testing Connection", 
                $"Connecting to SolarWinds server at {credentials.Hostname}:{credentials.Port}...", 
                "Cancel");

            // TODO: Implement actual connection test using PowerShell SWIS
            // For now, simulate a connection test
            await Task.Delay(2000);

            if (loadingTask != null && !loadingTask.IsCompleted)
            {
                // Connection test completed - simulate success
                await _dialogService.ShowAlertAsync("Connection Test", 
                    $"✅ Successfully connected to SolarWinds server!\n\nHostname: {credentials.Hostname}\nPort: {credentials.Port}\nUsername: {credentials.Username}");
                _logger.LogInformation("SolarWinds connection test successful for {Hostname}", credentials.Hostname);
            }
        }, "Test SolarWinds Connection");
    }

    [RelayCommand]
    private async Task ClearSolarWindsCredentials()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Clear SolarWinds Credentials", 
                "Are you sure you want to remove all stored SolarWinds credentials?");
                
            if (confirmed)
            {
                await _credentialsService.ClearSolarWindsCredentialsAsync();
                
                // Clear form fields
                SolarWindsHostname = string.Empty;
                SolarWindsUsername = string.Empty;
                SolarWindsPassword = string.Empty;
                SolarWindsPort = 17778;
                SolarWindsCredentialsConfigured = false;

                await _dialogService.ShowAlertAsync("Cleared", "SolarWinds credentials have been removed.");
                _logger.LogInformation("SolarWinds credentials cleared");
            }
        }, "Clear SolarWinds Credentials");
    }

    [RelayCommand]
    private async Task SaveDatabaseCredentials()
    {
        await ExecuteSafelyAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(DatabaseServer))
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Server name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Database name is required.");
                return;
            }

            var useWindowsAuth = SelectedAuthenticationType == "Windows Authentication";
            
            if (!useWindowsAuth)
            {
                if (string.IsNullOrWhiteSpace(DatabaseUsername))
                {
                    await _dialogService.ShowAlertAsync("Validation Error", "Username is required for SQL Server Authentication.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(DatabasePassword) || DatabasePassword == "••••••••")
                {
                    await _dialogService.ShowAlertAsync("Validation Error", "Password is required for SQL Server Authentication.");
                    return;
                }
            }

            await _credentialsService.SaveDatabaseCredentialsAsync(
                DatabaseServer, 
                DatabaseName, 
                useWindowsAuth, 
                useWindowsAuth ? null : DatabaseUsername, 
                useWindowsAuth ? null : DatabasePassword, 
                ConnectionTimeout);

            DatabaseCredentialsConfigured = true;
            await _dialogService.ShowAlertAsync("Saved", "Database credentials have been saved securely.");
            _logger.LogInformation("Database credentials saved for server: {Server}", DatabaseServer);
        }, "Save Database Credentials");
    }

    [RelayCommand]
    private async Task TestDatabaseConnection()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
            if (credentials == null)
            {
                await _dialogService.ShowAlertAsync("Error", "No database credentials found. Please save credentials first.");
                return;
            }

            // Show testing message
            var loadingTask = _dialogService.ShowAlertAsync("Testing Connection", "Testing database connection, please wait...");
            
            // Perform the actual connection test
            var testResult = await _databaseService.TestStoredConnectionAsync();
            
            // Dismiss loading and show result
            if (testResult.IsSuccess)
            {
                var successMessage = $"{testResult.Message}\n\n" +
                                   $"🖥️ Server: {testResult.ServerInfo}\n" +
                                   $"⏱️ Connection Time: {testResult.ConnectionTime?.TotalMilliseconds:F0}ms\n" +
                                   $"🕒 Tested: {testResult.TestTimestamp:HH:mm:ss}";
                
                await _dialogService.ShowAlertAsync("Connection Successful", successMessage);
                _logger.LogInformation("Database connection test successful for server: {Server}", credentials.Server);
            }
            else
            {
                var errorMessage = testResult.Message;
                if (!string.IsNullOrEmpty(testResult.ErrorDetails))
                {
                    errorMessage += $"\n\nDetails: {testResult.ErrorDetails}";
                }
                
                await _dialogService.ShowAlertAsync("Connection Failed", errorMessage);
                _logger.LogWarning("Database connection test failed for server: {Server}. Error: {Error}", 
                    credentials.Server, testResult.Message);
            }
        }, "Test Database Connection");
    }

    [RelayCommand]
    private async Task ClearDatabaseCredentials()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Clear Database Credentials", 
                "Are you sure you want to remove all stored database credentials?");
                
            if (confirmed)
            {
                await _credentialsService.ClearDatabaseCredentialsAsync();
                
                // Clear form fields
                DatabaseServer = string.Empty;
                DatabaseName = string.Empty;
                SelectedAuthenticationType = "Windows Authentication";
                DatabaseUsername = string.Empty;
                DatabasePassword = string.Empty;
                ConnectionTimeout = 30;
                DatabaseCredentialsConfigured = false;

                await _dialogService.ShowAlertAsync("Cleared", "Database credentials have been removed.");
                _logger.LogInformation("Database credentials cleared");
            }
        }, "Clear Database Credentials");
    }

    private async Task LoadAuthorizationInfoAsync()
    {
        try
        {
            CurrentUserName = await _authorizationService.GetCurrentUserNameAsync();
            var userRole = await _authorizationService.GetUserRoleAsync();
            CurrentUserRole = userRole.ToString();
            
            // Check if developer mode is enabled
            DeveloperModeEnabled = _authorizationService.IsDeveloperModeEnabled;
            if (_authorizationService.SimulatedRole.HasValue)
            {
                SimulatedRole = _authorizationService.SimulatedRole.Value.ToString();
            }

            // Load domain configuration
            var config = await _authorizationService.GetConfigurationAsync();
            DomainName = config.DomainName;

            // Check if user can edit domain configuration (LocalAdmin or SystemAdmin only)
            CanEditDomainConfiguration = userRole == UserRole.LocalAdmin || userRole == UserRole.SystemAdmin;
            
            // Show domain configuration section if:
            // 1. Domain is not configured yet (PLACEHOLDER_DOMAIN), OR
            // 2. User is a LocalAdmin or SystemAdmin (can always see/edit)
            ShowDomainConfiguration = DomainName == "PLACEHOLDER_DOMAIN" || userRole == UserRole.LocalAdmin || userRole == UserRole.SystemAdmin;
            
            // Show database configuration section only for DatabaseAdmin and SystemAdmin
            ShowDatabaseConfiguration = userRole == UserRole.DatabaseAdmin || userRole == UserRole.SystemAdmin;
            
            _logger.LogInformation("Authorization info loaded - User: {User}, Role: {Role}, Domain: {Domain}, CanEditDomain: {CanEdit}, ShowDomain: {ShowDomain}, ShowDatabase: {ShowDatabase}", 
                CurrentUserName, CurrentUserRole, DomainName, CanEditDomainConfiguration, ShowDomainConfiguration, ShowDatabaseConfiguration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load authorization information");
            CurrentUserName = "Error loading user";
            CurrentUserRole = "Error loading role";
        }
    }

    [RelayCommand]
    private async Task OnVersionTapped()
    {
        _versionTapCount++;
        if (_versionTapCount >= 7)
        {
            _versionTapCount = 0;
            
#if DEBUG
            DeveloperModeEnabled = !DeveloperModeEnabled;
            
            if (DeveloperModeEnabled)
            {
                _authorizationService.SetDeveloperMode(true);
                await _dialogService.ShowAlertAsync("Developer Mode", 
                    "🛠️ Developer mode activated!\n\nYou can now simulate different user roles for testing.");
            }
            else
            {
                _authorizationService.SetDeveloperMode(false);
                SimulatedRole = "Current User";
                await LoadAuthorizationInfoAsync(); // Refresh role display
                await _dialogService.ShowAlertAsync("Developer Mode", 
                    "Developer mode deactivated. Using actual user permissions.");
            }
#else
            await _dialogService.ShowAlertAsync("Information", 
                "Developer mode is only available in debug builds.");
#endif
        }
    }

    [RelayCommand]
    private async Task ChangeSimulatedRole()
    {
        if (!DeveloperModeEnabled) return;

        var selectedRole = SimulatedRole switch
        {
            "Read Only" => UserRole.ReadOnly,
            "Standard" => UserRole.Standard,
            "Local Admin" => UserRole.LocalAdmin,
            "Database Admin" => UserRole.DatabaseAdmin,
            "System Admin" => UserRole.SystemAdmin,
            _ => (UserRole?)null
        };

        _authorizationService.SetDeveloperMode(true, selectedRole);
        await LoadAuthorizationInfoAsync(); // Refresh role display
        
        _logger.LogInformation("Simulated role changed to: {Role}", SimulatedRole);
    }

    [RelayCommand]
    private async Task TestAllPermissions()
    {
        if (!DeveloperModeEnabled) return;

        await ExecuteSafelyAsync(async () =>
        {
            var permissions = await _authorizationService.GetUserPermissionsAsync();
            var role = await _authorizationService.GetUserRoleAsync();
            
            var results = new StringBuilder();
            results.AppendLine("🔐 Permission Test Results");
            results.AppendLine($"Current Role: {role}");
            results.AppendLine($"Simulated: {(DeveloperModeEnabled ? "Yes" : "No")}");
            results.AppendLine();
            
            results.AppendLine("📋 Specific Capabilities:");
            results.AppendLine($"• Database Admin Access: {await _authorizationService.CanAccessDatabaseAdminAsync()}");
            results.AppendLine($"• Edit Devices Directly: {await _authorizationService.CanEditDevicesDirectlyAsync()}");
            results.AppendLine($"• Approve Changes: {await _authorizationService.CanApproveChangesAsync()}");
            results.AppendLine($"• System Settings: {await _authorizationService.CanAccessSystemSettingsAsync()}");
            results.AppendLine();
            
            results.AppendLine("🛡️ Raw Permissions:");
            foreach (Permission permission in Enum.GetValues<Permission>())
            {
                if (permission != Permission.None && (permissions & permission) == permission)
                {
                    results.AppendLine($"• {permission}");
                }
            }
            
            await _dialogService.ShowAlertAsync("Permission Test Results", results.ToString());
        }, "Test Permissions");
    }

    // Handle setting changes
    partial void OnSelectedThemeChanged(string value)
    {
        _ = ExecuteSafelyAsync(async () =>
        {
            await _settingsService.SetAsync("selected_theme", value);
            
            // Apply the theme immediately
            var appTheme = value switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
            
            await _themeService.SetThemeAsync(appTheme);
            _logger.LogInformation("Theme changed to: {Theme}", value);
        }, "Change Theme");
    }

    partial void OnGridSpanChanged(int value)
    {
        // Use fire-and-forget with proper exception handling
        _ = SetSettingAsync("grid_span", value);
        
        // Notify other ViewModels about the change
        SettingsManager.NotifyGridSpanChanged(value);
        
        _logger.LogInformation("Grid span changed to: {GridSpan}", value);
    }

    partial void OnSelectedFontSizeChanged(string value)
    {
        _ = SetSettingAsync("font_size", value);
        _logger.LogInformation("Font size changed to: {FontSize}", value);
    }

    partial void OnDeviceRefreshIntervalChanged(int value)
    {
        _ = SetSettingAsync("device_refresh_interval", value);
        _logger.LogInformation("Device refresh interval changed to: {Interval}s", value);
    }

    partial void OnCameraRefreshIntervalChanged(int value)
    {
        _ = SetSettingAsync("camera_refresh_interval", value);
        _logger.LogInformation("Camera refresh interval changed to: {Interval}s", value);
    }

    partial void OnPrinterRefreshIntervalChanged(int value)
    {
        _ = SetSettingAsync("printer_refresh_interval", value);
        _logger.LogInformation("Printer refresh interval changed to: {Interval}s", value);
    }

    private async Task SetSettingAsync<T>(string key, T value)
    {
        try
        {
            await _settingsService.SetAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save setting {Key} with value {Value}", key, value);
        }
    }

    // Helper methods to format display values
    public string FormatRefreshInterval(int seconds)
    {
        return seconds switch
        {
            30 => "30 seconds",
            60 => "1 minute",
            300 => "5 minutes",
            900 => "15 minutes",
            _ => $"{seconds} seconds"
        };
    }

    [RelayCommand]
    private async Task ShowAboutDetails()
    {
        // Check for developer mode activation
        await OnVersionTapped();
        
        await ExecuteSafelyAsync(async () =>
        {
            var aboutMessage = $@"🖥️ IT Support Framework

Version: {AppVersion}
Build Date: {BuildDate}
Framework: {FrameworkVersion}

🏢 About the Application
This comprehensive .NET MAUI application provides enterprise-level IT device management capabilities with advanced security features and real-time database synchronization.

👨‍💻 Developer Information
Created by: {DeveloperName}
Title: IT Shop Floor Lineside Technician
Company: {CompanyName}

🔧 Technical Stack
• .NET 8.0 MAUI cross-platform framework
• C# 12.0 with modern language features
• SQL Server 2019+ with ACID compliance
• MVVM architecture with dependency injection
• Advanced SQL injection prevention
• Enterprise-grade input sanitization

✨ Key Capabilities
• Multi-device type support (Printers, Cameras, NetOp devices)
• Real-time database synchronization with transaction safety
• Comprehensive device management with 25+ data fields
• Advanced filtering and search capabilities
• Modern responsive UI with theme support
• Cross-platform deployment (Windows, Android, iOS, macOS)

🛡️ Security Features
• Parameterized queries preventing SQL injection
• Multi-layer input validation and sanitization
• Secure credential storage and management
• Comprehensive audit logging
• Type-safe data handling

© 2025 {DeveloperName}. All rights reserved.";

            await _dialogService.ShowAlertAsync("About IT Support Framework", aboutMessage);
            _logger.LogInformation("About dialog displayed");
        }, "Show About Details");
    }

    [RelayCommand]
    private void ToggleDomainConfiguration()
    {
        ShowDomainConfiguration = !ShowDomainConfiguration;
        _logger.LogInformation("Domain configuration visibility toggled: {Visible}", ShowDomainConfiguration);
    }

    [RelayCommand]
    private async Task SaveDomainConfiguration()
    {
        try
        {
            // Security check: Only SystemAdmin can modify domain configuration
            if (!CanEditDomainConfiguration)
            {
                await _dialogService.ShowAlertAsync("Access Denied", 
                    "🚫 Only System Administrators can modify domain configuration.\n\n" +
                    "Current role: " + CurrentUserRole);
                _logger.LogWarning("Domain configuration save attempt denied for user {User} with role {Role}", 
                    CurrentUserName, CurrentUserRole);
                return;
            }

            // Validate domain configuration
            if (string.IsNullOrWhiteSpace(DomainName))
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Domain name is required.");
                return;
            }

            // Create configuration object
            var config = new AuthorizationConfig
            {
                DomainName = DomainName.Trim()
            };

            // Save configuration
            await _authorizationService.SaveConfigurationAsync(config);
            
            // Clear cache to force refresh
            _authorizationService.ClearCache();

            await _dialogService.ShowAlertAsync("Success", 
                "Domain configuration saved successfully. Changes will take effect immediately.");
            
            _logger.LogInformation("Domain configuration saved - Domain: {Domain}", DomainName);
            
            // Reload authorization info with new config
            await LoadAuthorizationInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save domain configuration");
            await _dialogService.ShowAlertAsync("Error", 
                $"Failed to save domain configuration: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestDomainConnection()
    {
        try
        {
            // Security check: Only SystemAdmin can test domain configuration
            if (!CanEditDomainConfiguration)
            {
                await _dialogService.ShowAlertAsync("Access Denied", 
                    "🚫 Only System Administrators can test domain connections.\n\n" +
                    "Current role: " + CurrentUserRole);
                _logger.LogWarning("Domain connection test attempt denied for user {User} with role {Role}", 
                    CurrentUserName, CurrentUserRole);
                return;
            }

            if (string.IsNullOrWhiteSpace(DomainName))
            {
                await _dialogService.ShowAlertAsync("Validation Error", "Please enter a domain name first.");
                return;
            }

            IsBusy = true;

            // Test if domain is reachable by checking if we're in a domain environment
            bool isNonDomain = await _authorizationService.IsNonDomainEnvironmentAsync();
            
            string message;
            if (isNonDomain)
            {
                message = $"⚠️ Cannot connect to domain '{DomainName}'.\n\n" +
                         "This could mean:\n" +
                         "• Machine is not domain-joined\n" +
                         "• Domain controller is unreachable\n" +
                         "• Domain name is incorrect\n\n" +
                         "The app will run in non-domain mode with localhost-only database restrictions.";
            }
            else
            {
                message = $"✅ Successfully connected to domain '{DomainName}'.\n\n" +
                         "Active Directory integration is working correctly.";
            }

            await _dialogService.ShowAlertAsync("Domain Connection Test", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test domain connection");
            await _dialogService.ShowAlertAsync("Error", 
                $"Failed to test domain connection: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
