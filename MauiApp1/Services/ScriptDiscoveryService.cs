using MauiApp1.Models;
using MauiApp1.Scripts;
using MauiApp1.Interfaces;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MauiApp1.Services;

/// <summary>
/// Implementation of script discovery service
/// </summary>
public class ScriptDiscoveryService : IScriptDiscoveryService
{
    private readonly ILogger<ScriptDiscoveryService> _logger;
    private readonly Dictionary<string, IAutomationScript> _scriptInstances = new();
    private readonly Dictionary<string, IScript> _simpleScriptInstances = new();
    private readonly string _scriptsPath;

    public ScriptDiscoveryService(ILogger<ScriptDiscoveryService> logger)
    {
        _logger = logger;
        _scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
    }

    /// <summary>
    /// Discover all scripts by scanning assemblies and instantiating script classes
    /// </summary>
    public Task<List<Script>> DiscoverScriptsAsync()
    {
        var scripts = new List<Script>();
        
        try
        {
            _logger.LogInformation("Starting script discovery in: {ScriptsPath}", _scriptsPath);
            Console.WriteLine($"[ScriptDiscovery] Starting script discovery in: {_scriptsPath}");

            // Get all types that implement IAutomationScript from the current assembly
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();
            _logger.LogInformation("Assembly has {Count} total types", allTypes.Length);
            Console.WriteLine($"[ScriptDiscovery] Assembly has {allTypes.Length} total types");
            
            // Log some type names for debugging
            var typeNames = allTypes.Select(t => t.FullName).Take(10);
            _logger.LogDebug("First 10 type names: {TypeNames}", string.Join(", ", typeNames));
            Console.WriteLine($"[ScriptDiscovery] First 10 type names: {string.Join(", ", typeNames)}");
            
            var scriptTypes = allTypes
                .Where(t => typeof(IAutomationScript).IsAssignableFrom(t) && 
                           !t.IsInterface && 
                           !t.IsAbstract)
                .ToList();

            _logger.LogInformation("Found {Count} IAutomationScript types in assembly", scriptTypes.Count);
            Console.WriteLine($"[ScriptDiscovery] Found {scriptTypes.Count} IAutomationScript types in assembly");
            
            // Also discover simple IScript implementations
            var simpleScriptTypes = allTypes
                .Where(t => typeof(IScript).IsAssignableFrom(t) && 
                           !t.IsInterface && 
                           !t.IsAbstract)
                .ToList();

            _logger.LogInformation("Found {Count} simple IScript types in assembly", simpleScriptTypes.Count);
            Console.WriteLine($"[ScriptDiscovery] Found {simpleScriptTypes.Count} simple IScript types in assembly");
            
            // Log each script type found
            foreach (var type in scriptTypes)
            {
                _logger.LogInformation("Found script type: {TypeName} in namespace: {Namespace}", type.Name, type.Namespace);
                Console.WriteLine($"[ScriptDiscovery] Found script type: {type.Name} in namespace: {type.Namespace}");
            }
            
            // Also check if ComputerInfoScript specifically exists
            var computerInfoType = allTypes.FirstOrDefault(t => t.Name == "ComputerInfoScript");
            if (computerInfoType != null)
            {
                _logger.LogInformation("ComputerInfoScript found: {FullName}, IsAssignableFrom IAutomationScript: {IsAssignable}, IsInterface: {IsInterface}, IsAbstract: {IsAbstract}", 
                    computerInfoType.FullName, 
                    typeof(IAutomationScript).IsAssignableFrom(computerInfoType), 
                    computerInfoType.IsInterface, 
                    computerInfoType.IsAbstract);
                Console.WriteLine($"[ScriptDiscovery] ComputerInfoScript found: {computerInfoType.FullName}");
                Console.WriteLine($"[ScriptDiscovery] - IsAssignableFrom IAutomationScript: {typeof(IAutomationScript).IsAssignableFrom(computerInfoType)}");
                Console.WriteLine($"[ScriptDiscovery] - IsInterface: {computerInfoType.IsInterface}");
                Console.WriteLine($"[ScriptDiscovery] - IsAbstract: {computerInfoType.IsAbstract}");
            }
            else
            {
                _logger.LogWarning("ComputerInfoScript not found in assembly types");
                Console.WriteLine("[ScriptDiscovery] WARNING: ComputerInfoScript not found in assembly types");
            }

            foreach (var scriptType in scriptTypes)
            {
                try
                {
                    _logger.LogInformation("Attempting to instantiate script type: {ScriptType}", scriptType.FullName);
                    Console.WriteLine($"[ScriptDiscovery] Attempting to instantiate script type: {scriptType.FullName}");
                    
                    // Create instance using dependency injection if possible, otherwise use Activator
                    IAutomationScript? scriptInstance = null;
                    
                    // Try to create with logger parameter (most scripts will inherit from BaseAutomationScript)
                    var constructors = scriptType.GetConstructors();
                    _logger.LogDebug("Script type {ScriptType} has {Count} constructors", scriptType.Name, constructors.Length);
                    Console.WriteLine($"[ScriptDiscovery] Script type {scriptType.Name} has {constructors.Length} constructors");
                    
                    // Log constructor details for debugging
                    foreach (var ctor in constructors)
                    {
                        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType.Name).ToArray();
                        _logger.LogDebug("Constructor for {ScriptType}: ({Parameters})", scriptType.Name, string.Join(", ", paramTypes));
                        Console.WriteLine($"[ScriptDiscovery] Constructor for {scriptType.Name}: ({string.Join(", ", paramTypes)})");
                    }
                    
                    var loggerConstructor = constructors.FirstOrDefault(c => 
                        c.GetParameters().Length == 1 && 
                        (c.GetParameters()[0].ParameterType == typeof(ILogger) ||
                         c.GetParameters()[0].ParameterType.IsGenericType &&
                         c.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ILogger<>)));

                    if (loggerConstructor != null)
                    {
                        _logger.LogDebug("Using logger constructor for {ScriptType}", scriptType.Name);
                        Console.WriteLine($"[ScriptDiscovery] Using logger constructor for {scriptType.Name}");
                        // Create logger for this specific script type
                        var loggerType = typeof(ILogger<>).MakeGenericType(scriptType);
                        var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
                        var logger = loggerFactory.CreateLogger(scriptType);
                        
                        scriptInstance = (IAutomationScript)Activator.CreateInstance(scriptType, logger)!;
                    }
                    else
                    {
                        _logger.LogDebug("Using parameterless constructor for {ScriptType}", scriptType.Name);
                        Console.WriteLine($"[ScriptDiscovery] Using parameterless constructor for {scriptType.Name}");
                        // Try parameterless constructor
                        scriptInstance = (IAutomationScript)Activator.CreateInstance(scriptType)!;
                    }

                    if (scriptInstance != null)
                    {
                        _logger.LogInformation("Successfully created script instance: {ScriptName} (ID: {ScriptId})", scriptInstance.Name, scriptInstance.Id);
                        Console.WriteLine($"[ScriptDiscovery] Successfully created script instance: {scriptInstance.Name} (ID: {scriptInstance.Id})");
                        _scriptInstances[scriptInstance.Id] = scriptInstance;

                        var script = new Script
                        {
                            Id = GetNextId(),
                            Name = scriptInstance.Name,
                            Description = scriptInstance.Description,
                            Category = scriptInstance.Category,
                            RequiresAdmin = scriptInstance.RequiresAdmin,
                            EstimatedDuration = scriptInstance.EstimatedDuration,
                            Author = scriptInstance.Author,
                            Version = scriptInstance.Version,
                            ExecutionMethod = scriptInstance.Id,
                            IsEnabled = true,
                            LastModified = DateTime.Now
                        };

                        scripts.Add(script);
                        _logger.LogDebug("Discovered script: {ScriptName} ({ScriptId})", script.Name, script.ExecutionMethod);
                        Console.WriteLine($"[ScriptDiscovery] Discovered script: {script.Name} ({script.ExecutionMethod})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to instantiate script type: {ScriptType}", scriptType.Name);
                    Console.WriteLine($"[ScriptDiscovery] ERROR: Failed to instantiate script type: {scriptType.Name} - {ex.Message}");
                }
            }

            // Process simple IScript implementations
            foreach (var simpleScriptType in simpleScriptTypes)
            {
                try
                {
                    _logger.LogInformation("Processing simple script type: {ScriptType}", simpleScriptType.FullName);
                    Console.WriteLine($"[ScriptDiscovery] Processing simple script type: {simpleScriptType.FullName}");
                    
                    // Create instance of simple script
                    var simpleScriptInstance = (IScript)Activator.CreateInstance(simpleScriptType)!;
                    
                    if (simpleScriptInstance != null)
                    {
                        _logger.LogInformation("Successfully created simple script instance: {ScriptName}", simpleScriptInstance.ScriptName);
                        Console.WriteLine($"[ScriptDiscovery] Successfully created simple script instance: {simpleScriptInstance.ScriptName}");
                        
                        // Convert simple script to Script model for UI
                        var script = new Script
                        {
                            Name = simpleScriptInstance.ScriptName,
                            Description = simpleScriptInstance.Description,
                            Category = simpleScriptInstance.Category, // Use the actual category from the script
                            RequiresAdmin = false, // Simple scripts don't require admin by default
                            Author = string.IsNullOrEmpty(simpleScriptInstance.Author) ? "System" : simpleScriptInstance.Author,
                            EstimatedDuration = "< 1 minute",
                            Version = "1.0",
                            ExecutionMethod = $"simple:{simpleScriptType.FullName}", // Mark as simple script
                            IsEnabled = true,
                            LastModified = DateTime.Now
                        };

                        scripts.Add(script);
                        
                        // Store the simple script instance for execution
                        _simpleScriptInstances[script.ExecutionMethod] = simpleScriptInstance;
                        
                        _logger.LogDebug("Discovered simple script: {ScriptName} ({ExecutionMethod})", script.Name, script.ExecutionMethod);
                        Console.WriteLine($"[ScriptDiscovery] Discovered simple script: {script.Name} ({script.ExecutionMethod})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to instantiate simple script type: {ScriptType}", simpleScriptType.Name);
                    Console.WriteLine($"[ScriptDiscovery] ERROR: Failed to instantiate simple script type: {simpleScriptType.Name} - {ex.Message}");
                }
            }

            _logger.LogInformation("Script discovery completed. Found {Count} scripts", scripts.Count);
            Console.WriteLine($"[ScriptDiscovery] Script discovery completed. Found {scripts.Count} scripts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during script discovery");
            Console.WriteLine($"[ScriptDiscovery] FATAL ERROR during script discovery: {ex.Message}");
        }

        return Task.FromResult(scripts);
    }

    /// <summary>
    /// Execute a script by its ID
    /// </summary>
    public async Task<Services.ScriptExecutionResult> ExecuteScriptAsync(string scriptId, Dictionary<string, object>? parameters = null)
    {
        // Check for complex IAutomationScript first
        if (_scriptInstances.TryGetValue(scriptId, out var script))
        {
            var result = await script.ExecuteAsync(parameters);
            // Convert from Scripts.ScriptExecutionResult to Services.ScriptExecutionResult
            return new Services.ScriptExecutionResult
            {
                Success = result.Success,
                Message = result.Success ? result.Output : result.Error,
                Data = result.Data,
                ExecutionTime = result.ExecutionTime
            };
        }

        // Check for simple IScript
        if (_simpleScriptInstances.TryGetValue(scriptId, out var simpleScript))
        {
            try
            {
                _logger.LogInformation("Executing simple script: {ScriptName}", simpleScript.ScriptName);
                Console.WriteLine($"[ScriptDiscovery] Executing simple script: {simpleScript.ScriptName}");
                
                // Execute the simple script (void method, so we capture output via console redirection)
                var originalConsoleOut = Console.Out;
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);
                
                simpleScript.Execute();
                
                Console.SetOut(originalConsoleOut);
                var output = stringWriter.ToString();
                
                _logger.LogInformation("Simple script executed successfully: {ScriptName}", simpleScript.ScriptName);
                Console.WriteLine($"[ScriptDiscovery] Simple script executed successfully: {simpleScript.ScriptName}");
                
                return new Services.ScriptExecutionResult
                {
                    Success = true,
                    Message = output,
                    ExecutionTime = TimeSpan.FromSeconds(1) // Approximate for simple scripts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing simple script: {ScriptName}", simpleScript.ScriptName);
                Console.WriteLine($"[ScriptDiscovery] ERROR executing simple script: {simpleScript.ScriptName} - {ex.Message}");
                
                return new Services.ScriptExecutionResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        return new Services.ScriptExecutionResult
        {
            Success = false,
            Message = $"Script with ID '{scriptId}' not found"
        };
    }

    /// <summary>
    /// Get a script instance by ID
    /// </summary>
    public Task<IScript?> GetScriptAsync(string scriptId)
    {
        if (_simpleScriptInstances.TryGetValue(scriptId, out var simpleScript))
        {
            return Task.FromResult<IScript?>(simpleScript);
        }

        return Task.FromResult<IScript?>(null);
    }

    /// <summary>
    /// Validate a script can be executed
    /// </summary>
    public Task<Services.ScriptValidationResult> ValidateScriptAsync(string scriptId)
    {
        if (_simpleScriptInstances.TryGetValue(scriptId, out var simpleScript))
        {
            // Simple scripts are generally always valid if they implement IScript
            return Task.FromResult(new Services.ScriptValidationResult
            {
                IsValid = true,
                Message = $"Script '{simpleScript.ScriptName}' is valid"
            });
        }

        return Task.FromResult(new Services.ScriptValidationResult
        {
            IsValid = false,
            ValidationErrors = { $"Script with ID '{scriptId}' not found" }
        });
    }

    private static int _nextId = 1;
    private static int GetNextId() => _nextId++;
}
