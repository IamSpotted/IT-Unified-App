namespace MauiApp1.ViewModels;

/// <summary>
/// Base ViewModel for filterable collections
/// </summary>
public abstract partial class FilterableBaseViewModel<T> : BaseViewModel where T : class, IFilterable
{
    protected readonly IFilterService<T> _filterService;

    [ObservableProperty]
    private ObservableCollection<T> _items = new();

    [ObservableProperty]
    private ObservableCollection<T> _filteredItems = new();

    [ObservableProperty]
    private Dictionary<string, ObservableCollection<string>> _filterOptions = new();

    [ObservableProperty]
    private Dictionary<string, string> _selectedFilters = new();

    [ObservableProperty]
    private bool _hasActiveFilters = false;

    [ObservableProperty]
    private string _searchText = string.Empty;

    protected FilterableBaseViewModel(
        ILogger logger,
        INavigationService navigationService,
        IDialogService dialogService,
        IFilterService<T> filterService)
        : base(logger, navigationService, dialogService)
    {
        _filterService = filterService;
        InitializeFilters();
    }

    protected abstract string[] GetFilterProperties();

    private void InitializeFilters()
    {
        var properties = GetFilterProperties();
        foreach (var property in properties)
        {
            FilterOptions[property] = new ObservableCollection<string> { "" };
            SelectedFilters[property] = "";
        }
    }

    protected void UpdateFilterOptions()
    {
        var properties = GetFilterProperties();
        var availableValues = _filterService.GetAvailableFilterValues(Items, properties);

        foreach (var property in properties)
        {
            if (availableValues.TryGetValue(property, out var values))
            {
                FilterOptions[property].Clear();
                foreach (var value in values)
                {
                    FilterOptions[property].Add(value);
                }
            }
        }
    }

    [RelayCommand]
    protected virtual void ApplyFilters()
    {
        var filtered = _filterService.ApplyFilters(Items, SelectedFilters);
        
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

        HasActiveFilters = SelectedFilters.Any(f => !string.IsNullOrEmpty(f.Value)) || !string.IsNullOrWhiteSpace(SearchText);
        
        _logger.LogInformation("Applied filters: {Count} items shown out of {Total}", 
            FilteredItems.Count, Items.Count);
    }

    protected abstract IEnumerable<T> ApplySearchFilter(IEnumerable<T> items, string searchText);

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    [RelayCommand]
    protected virtual void ClearFilters()
    {
        var properties = GetFilterProperties();
        foreach (var property in properties)
        {
            SelectedFilters[property] = "";
        }
        
        // Clear search text
        SearchText = string.Empty;
        
        // Notify UI that all Selected* properties have changed
        foreach (var property in properties)
        {
            OnPropertyChanged($"Selected{property}");
        }
        
        ApplyFilters();
    }

    protected void OnFilterChanged(string propertyName, string newValue)
    {
        SelectedFilters[propertyName] = newValue;
        ApplyFilters();
    }

    protected void LoadItems(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
        
        UpdateFilterOptions();
        ApplyFilters();
    }
}
