namespace MauiApp1.Interfaces;

/// <summary>
/// Generic interface for filterable entities
/// </summary>
public interface IFilterable
{
    string GetFilterValue(string filterProperty);
    bool MatchesFilter(string filterProperty, string filterValue);
}

/// <summary>
/// Interface for filter services
/// </summary>
public interface IFilterService<T> where T : IFilterable
{
    IEnumerable<T> ApplyFilters(IEnumerable<T> items, Dictionary<string, string> filters);
    Dictionary<string, List<string>> GetAvailableFilterValues(IEnumerable<T> items, string[] filterProperties);
}

/// <summary>
/// Interface for filter criteria
/// </summary>
public interface IFilterCriteria
{
    Dictionary<string, string> GetFilters();
    void ClearFilters();
    bool HasActiveFilters();
}
