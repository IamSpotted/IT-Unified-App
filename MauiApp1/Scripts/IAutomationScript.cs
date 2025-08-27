namespace MauiApp1.Scripts;

/// <summary>
/// Interface that all automation scripts must implement
/// </summary>
public interface IAutomationScript
{
    /// <summary>
    /// Unique identifier for the script
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display name of the script
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Description of what the script does
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Category of the script (System, Network, etc.)
    /// </summary>
    string Category { get; }
    
    /// <summary>
    /// Whether the script requires administrator privileges
    /// </summary>
    bool RequiresAdmin { get; }
    
    /// <summary>
    /// Estimated execution time
    /// </summary>
    string EstimatedDuration { get; }
    
    /// <summary>
    /// Author of the script
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// Version of the script
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Execute the script asynchronously
    /// </summary>
    /// <param name="parameters">Optional parameters for script execution</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Script execution result</returns>
    Task<ScriptExecutionResult> ExecuteAsync(Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate that the script can be executed in the current environment
    /// </summary>
    /// <returns>Validation result</returns>
    Task<ScriptValidationResult> ValidateAsync();
}

/// <summary>
/// Result of script execution
/// </summary>
public class ScriptExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.Now;
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Result of script validation
/// </summary>
public class ScriptValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
