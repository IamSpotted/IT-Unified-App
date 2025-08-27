namespace MauiApp1.Services;

/// <summary>
/// Generic filter service implementation
/// </summary>
public class FilterService<T> : IFilterService<T> where T : IFilterable
{
    private readonly ILogger<FilterService<T>> _logger;

    public FilterService(ILogger<FilterService<T>> logger)
    {
        _logger = logger;
    }

    public IEnumerable<T> ApplyFilters(IEnumerable<T> items, Dictionary<string, string> filters)
    {
        if (filters == null || !filters.Any())
            return items;

        var filtered = items.AsEnumerable();

        foreach (var filter in filters)
        {
            if (string.IsNullOrEmpty(filter.Value))
                continue;

            filtered = filtered.Where(item => item.MatchesFilter(filter.Key, filter.Value));
        }

        var result = filtered.ToList();
        _logger.LogDebug("Applied {FilterCount} filters: {ResultCount} items remaining", 
            filters.Count(f => !string.IsNullOrEmpty(f.Value)), result.Count);

        return result;
    }

    public Dictionary<string, List<string>> GetAvailableFilterValues(IEnumerable<T> items, string[] filterProperties)
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var property in filterProperties)
        {
            var values = items
                .Select(item => item.GetFilterValue(property))
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct()
                .OrderBy(value => value)
                .ToList();

            result[property] = values;
        }

        _logger.LogDebug("Generated filter options for {PropertyCount} properties", filterProperties.Length);
        return result;
    }
}
