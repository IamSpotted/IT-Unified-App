namespace MauiApp1.ViewModels;

/// <summary>
/// ViewModel for managing C# automation scripts within the IT support framework.
/// This ViewModel provides comprehensive script management functionality including execution,
/// filtering, and search operations for native C# automation scripts.
/// </summary>
public partial class ScriptsViewModel : FilterableBaseViewModel<Script>, IDisposable
{
    // Script service for script discovery and execution.
    private readonly IScriptDiscoveryService _scriptDiscoveryService;
    
    // Settings service for user preferences and configuration persistence.
    private readonly ISettingsService _settingsService;
    
    private bool _disposed = false;

    // Available filter options for UI binding
    [ObservableProperty]
    private ObservableCollection<string> _availableCategories = new() { "All", "System", "Network", "Maintenance", "Diagnostics", "Security", "Monitoring", "Deployment", "Utilities" };

    [ObservableProperty]
    private ObservableCollection<string> _adminRequiredOptions = new() { "All", "Admin Required", "No Admin Required" };

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _selectedAdminFilter = "All";

    // Currently selected script for detailed operations
    [ObservableProperty]
    private Script? _selectedScript;

    public ScriptsViewModel(
        ILogger<ScriptsViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        IFilterService<Script> filterService,
        ISettingsService settingsService,
        IScriptDiscoveryService scriptDiscoveryService) 
        : base(logger, navigationService, dialogService, filterService)
    {
        Title = "Scripts";
        _settingsService = settingsService;
        _scriptDiscoveryService = scriptDiscoveryService;
        
        // Initialize and load discovered scripts
        _ = InitializeAsync();
    }

    protected override string[] GetFilterProperties()
    {
        return new[] { "Category", "RequiresAdmin", "Author", "Enabled" };
    }

    /// <summary>
    /// Initialize the ViewModel asynchronously by discovering scripts
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            _logger.LogInformation("Initializing ScriptsViewModel with script discovery");

            // Discover scripts from the Scripts folder
            var discoveredScripts = await _scriptDiscoveryService.DiscoverScriptsAsync();
            
            _logger.LogInformation("Discovered {Count} scripts", discoveredScripts.Count);

            // Clear existing items and add discovered scripts
            foreach (var script in discoveredScripts.OrderBy(s => s.Category).ThenBy(s => s.Name))
            {
                Items.Add(script);
            }

            // If no scripts were discovered, show informational message
            if (!discoveredScripts.Any())
            {
                _logger.LogWarning("No scripts discovered. Scripts folder may be empty or scripts may not implement IAutomationScript interface.");
            }

            // Apply initial filtering
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ScriptsViewModel");
            await _dialogService.ShowAlertAsync("Error", $"Failed to load scripts: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadScripts()
    {
        await ExecuteSafelyAsync(async () =>
        {
            IsBusy = true;
            
            // Reload scripts from discovery service
            Items.Clear();
            var discoveredScripts = await _scriptDiscoveryService.DiscoverScriptsAsync();
            
            foreach (var script in discoveredScripts.OrderBy(s => s.Category).ThenBy(s => s.Name))
            {
                Items.Add(script);
            }
            
            ApplyFilters();
        }, "Load Scripts");
    }

    [RelayCommand]
    private async Task ExecuteScript(Script script)
    {
        if (script == null) return;

        await ExecuteSafelyAsync(async () =>
        {
            SelectedScript = script;
            
            // Check if this is a navigation script (simple script) vs automation script
            if (script.ExecutionMethod.StartsWith("simple:"))
            {
                // For navigation scripts, execute directly without confirmations
                _logger.LogInformation("Executing navigation script: {ScriptName}", script.Name);
                
                var result = await _scriptDiscoveryService.ExecuteScriptAsync(script.ExecutionMethod);
                
                // Update script execution info
                script.LastExecuted = DateTime.Now;
                script.LastResult = result.Success ? "Navigated successfully" : $"Failed: {result.Error}";
                
                return;
            }
            
            // For automation scripts, show confirmations
            // Check admin requirements
            if (script.RequiresAdmin)
            {
                var adminConfirm = await _dialogService.ShowConfirmationAsync(
                    "Admin Required", 
                    $"The script '{script.Name}' requires administrator privileges to run. Continue?",
                    "Run as Admin",
                    "Cancel");
                    
                if (!adminConfirm)
                    return;
            }

            // Show execution confirmation
            var executeConfirm = await _dialogService.ShowConfirmationAsync(
                "Execute Script",
                $"Execute '{script.Name}'?\n\nDescription: {script.Description}\n\nEstimated Duration: {script.EstimatedDuration}",
                "Execute",
                "Cancel");

            if (executeConfirm)
            {
                IsBusy = true;
                
                try
                {
                    _logger.LogInformation("Executing automation script: {ScriptName} (ID: {ScriptId})", script.Name, script.ExecutionMethod);
                    
                    // Execute the script using the discovery service
                    var result = await _scriptDiscoveryService.ExecuteScriptAsync(script.ExecutionMethod);
                    
                    // Update script execution info
                    script.LastExecuted = DateTime.Now;
                    script.LastResult = result.Success ? "Completed successfully" : $"Failed: {result.Error}";
                    
                    if (result.Success)
                    {
                        // Show output if available
                        var message = !string.IsNullOrEmpty(result.Output) 
                            ? $"Script '{script.Name}' executed successfully!\n\nOutput:\n{result.Output}"
                            : $"Script '{script.Name}' executed successfully!";
                            
                        await _dialogService.ShowAlertAsync("Script Completed", message);
                    }
                    else
                    {
                        await _dialogService.ShowAlertAsync("Script Failed", 
                            $"Script '{script.Name}' failed to execute.\n\nError: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute script: {ScriptName}", script.Name);
                    script.LastResult = $"Error: {ex.Message}";
                    await _dialogService.ShowAlertAsync("Execution Error", 
                        $"An error occurred while executing '{script.Name}':\n\n{ex.Message}");
                }
            }
        }, "Execute Script");
    }

    protected override IEnumerable<Script> ApplySearchFilter(IEnumerable<Script> items, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return items;

        return items.Where(s => s.MatchesSearch(searchText));
    }

    protected override void ClearFilters()
    {
        SelectedCategory = "All";
        SelectedAdminFilter = "All";
        base.ClearFilters();
    }

    protected override void ApplyFilters()
    {
        var filtered = Items.AsEnumerable();

        // Category filter
        if (SelectedCategory != "All")
        {
            filtered = filtered.Where(s => s.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        // Admin filter
        if (SelectedAdminFilter == "Admin Required")
        {
            filtered = filtered.Where(s => s.RequiresAdmin);
        }
        else if (SelectedAdminFilter == "No Admin Required")
        {
            filtered = filtered.Where(s => !s.RequiresAdmin);
        }

        // Apply search filter if search text is provided
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = ApplySearchFilter(filtered, SearchText);
        }

        FilteredItems.Clear();
        foreach (var item in filtered)
        {
            FilteredItems.Add(item);
        }

        HasActiveFilters = SelectedCategory != "All" || SelectedAdminFilter != "All" || !string.IsNullOrWhiteSpace(SearchText);
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedAdminFilterChanged(string value)
    {
        ApplyFilters();
    }

    // Additional property for UI binding compatibility
    public ObservableCollection<Script> FilteredScripts => FilteredItems;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Dispose managed resources
            Items?.Clear();
            FilteredItems?.Clear();
        }
        _disposed = true;
    }

    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
