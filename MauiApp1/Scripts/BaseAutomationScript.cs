using Microsoft.Extensions.Logging;
using System.Text;
using System.Diagnostics;
using System.Security.Principal;

namespace MauiApp1.Scripts;

/// <summary>
/// Base class for automation scripts providing common functionality
/// </summary>
public abstract class BaseAutomationScript : IAutomationScript
{
    /// <summary>
    /// Logger for script execution
    /// </summary>
    protected readonly ILogger Logger;

    protected BaseAutomationScript(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Unique identifier for the script (defaults to class name)
    /// </summary>
    public virtual string Id => GetType().Name;

    /// <summary>
    /// Display name of the script
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Description of what the script does
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Category of the script
    /// </summary>
    public abstract string Category { get; }

    /// <summary>
    /// Whether the script requires administrator privileges
    /// </summary>
    public abstract bool RequiresAdmin { get; }

    /// <summary>
    /// Estimated execution time
    /// </summary>
    public abstract string EstimatedDuration { get; }

    /// <summary>
    /// Author of the script (defaults to "IT Support Team")
    /// </summary>
    public virtual string Author => "IT Support Team";

    /// <summary>
    /// Version of the script (defaults to "1.0")
    /// </summary>
    public virtual string Version => "1.0";

    /// <summary>
    /// Execute the script with timing and error handling
    /// </summary>
    public async Task<ScriptExecutionResult> ExecuteAsync(Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        var result = new ScriptExecutionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogInformation("Starting execution of script: {ScriptName}", Name);
            
            // Validate before execution
            var validation = await ValidateAsync();
            if (!validation.IsValid)
            {
                result.Success = false;
                result.Error = string.Join("; ", validation.Errors);
                return result;
            }

            // Execute the actual script logic
            result = await ExecuteInternalAsync(parameters, cancellationToken);
            result.ExecutionTime = stopwatch.Elapsed;
            
            Logger.LogInformation("Script {ScriptName} completed in {Duration}ms. Success: {Success}", 
                Name, stopwatch.ElapsedMilliseconds, result.Success);
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Error = "Script execution was cancelled";
            Logger.LogWarning("Script {ScriptName} was cancelled", Name);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.ExecutionTime = stopwatch.Elapsed;
            Logger.LogError(ex, "Script {ScriptName} failed with exception", Name);
        }
        finally
        {
            stopwatch.Stop();
        }

        return result;
    }

    /// <summary>
    /// Validate that the script can be executed (default implementation)
    /// </summary>
    public virtual Task<ScriptValidationResult> ValidateAsync()
    {
        var result = new ScriptValidationResult { IsValid = true };

        // Check if running as administrator when required
        if (RequiresAdmin && !IsRunningAsAdministrator())
        {
            result.IsValid = false;
            result.Errors.Add("This script requires administrator privileges");
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Abstract method that derived classes must implement for actual script logic
    /// </summary>
    protected abstract Task<ScriptExecutionResult> ExecuteInternalAsync(Dictionary<string, object>? parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Helper method to check if running as administrator
    /// </summary>
    protected static bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Helper method to run system commands
    /// </summary>
    protected async Task<(bool Success, string Output, string Error)> RunCommandAsync(string command, string arguments = "", CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) => {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (sender, e) => {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var success = process.ExitCode == 0;
            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();

            return (success, output, error);
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

    /// <summary>
    /// Helper method to execute commands and return ScriptExecutionResult
    /// </summary>
    protected async Task<ScriptExecutionResult> ExecuteCommandAsync(string commandLine, CancellationToken cancellationToken = default)
    {
        try
        {
            var parts = commandLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0];
            var arguments = parts.Length > 1 ? parts[1] : "";

            var (success, output, error) = await RunCommandAsync(command, arguments, cancellationToken);

            return new ScriptExecutionResult
            {
                Success = success,
                Output = output,
                Error = error
            };
        }
        catch (Exception ex)
        {
            return new ScriptExecutionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}
