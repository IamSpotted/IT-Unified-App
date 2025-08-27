using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.Services
{
    /// <summary>
    /// Interface for script discovery service that works with IScript implementations
    /// </summary>
    public interface IScriptDiscoveryService : ISingletonService
    {
        /// <summary>
        /// Discover all available scripts that implement IScript
        /// </summary>
        Task<List<Script>> DiscoverScriptsAsync();
        
        /// <summary>
        /// Execute a specific script by ID
        /// </summary>
        Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptId, Dictionary<string, object>? parameters = null);
        
        /// <summary>
        /// Get a specific script by ID
        /// </summary>
        Task<IScript?> GetScriptAsync(string scriptId);
        
        /// <summary>
        /// Validate a script can be executed
        /// </summary>
        Task<ScriptValidationResult> ValidateScriptAsync(string scriptId);
    }

    /// <summary>
    /// Execution result for IScript implementations
    /// </summary>
    public class ScriptExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Error => Success ? string.Empty : Message;
        public string Output => Success ? Message : string.Empty;
        public object? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// Validation result for scripts
    /// </summary>
    public class ScriptValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
