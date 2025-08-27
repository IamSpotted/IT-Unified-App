namespace MauiApp1.Models;

/// <summary>
/// Represents a C# automation script in the system
/// </summary>
public class Script : IFilterable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool RequiresAdmin { get; set; }
    public string EstimatedDuration { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string Version { get; set; } = "1.0";
    
    // Execution properties
    public string ExecutionMethod { get; set; } = string.Empty; // Method name to invoke
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    
    // Status properties
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastExecuted { get; set; }
    public string LastResult { get; set; } = string.Empty;
    
    // Display helpers
    public string CategoryIcon => Category.ToLowerInvariant() switch
    {
        "system" => "🖥️",
        "network" => "🌐",
        "maintenance" => "🔧",
        "diagnostics" => "🔍",
        "security" => "🔒",
        "monitoring" => "📊",
        "deployment" => "📦",
        "utilities" => "⚙️",
        _ => "📜"
    };
    
    public string AdminIcon => RequiresAdmin ? "🔒" : "";
    
    public string FormattedLastExecuted => LastExecuted?.ToString("MM/dd/yyyy HH:mm") ?? "Never";

    // IFilterable implementation
    public string GetFilterValue(string filterProperty)
    {
        return filterProperty.ToLowerInvariant() switch
        {
            "category" => Category,
            "requiresadmin" => RequiresAdmin.ToString(),
            "author" => Author,
            "enabled" => IsEnabled.ToString(),
            _ => string.Empty
        };
    }

    public bool MatchesFilter(string filterProperty, string filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
            return true;

        var actualValue = GetFilterValue(filterProperty);
        return actualValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase);
    }

    public bool MatchesSearch(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;

        var term = searchTerm.ToLowerInvariant();
        return Name.ToLowerInvariant().Contains(term) ||
               Description.ToLowerInvariant().Contains(term) ||
               Category.ToLowerInvariant().Contains(term) ||
               Author.ToLowerInvariant().Contains(term);
    }
}
