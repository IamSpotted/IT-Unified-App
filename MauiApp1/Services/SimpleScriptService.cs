using System.Reflection;
using MauiApp1.Interfaces;
using MauiApp1.Services;

namespace MauiApp1.Services
{
    /// <summary>
    /// Simple script discovery service - based on working console app pattern
    /// </summary>
    public interface ISimpleScriptService : ISingletonService
    {
        List<IScript> GetAllScripts();
        void ExecuteScript(IScript script);
    }

    public class SimpleScriptService : ISimpleScriptService
    {
        private readonly List<IScript> _scripts;

        public SimpleScriptService()
        {
            _scripts = DiscoverScripts();
        }

        public List<IScript> GetAllScripts()
        {
            return _scripts;
        }

        public void ExecuteScript(IScript script)
        {
            try
            {
                script.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing script {script.ScriptName}: {ex.Message}");
                // TODO: In MAUI, show error dialog
            }
        }

        private List<IScript> DiscoverScripts()
        {
            var scripts = new List<IScript>();

            try
            {
                Console.WriteLine("[SimpleScriptService] Starting script discovery...");
                
                // Find all script types in the MauiApp1.Scripts namespace
                var allTypes = Assembly.GetExecutingAssembly().GetTypes();
                Console.WriteLine($"[SimpleScriptService] Found {allTypes.Length} total types in assembly");
                
                var scriptTypes = allTypes
                    .Where(t => t.Namespace == "MauiApp1.Scripts" && 
                               typeof(IScript).IsAssignableFrom(t) && 
                               !t.IsAbstract);

                Console.WriteLine($"[SimpleScriptService] Found {scriptTypes.Count()} script types");
                
                foreach (var type in scriptTypes)
                {
                    Console.WriteLine($"[SimpleScriptService] Processing script type: {type.Name}");
                    try
                    {
                        // Create instance of the script
                        var script = (IScript)Activator.CreateInstance(type)!;
                        scripts.Add(script);
                        
                        Console.WriteLine($"[SimpleScriptService] Successfully created script: {script.ScriptName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SimpleScriptService] Failed to create script {type.Name}: {ex.Message}");
                    }
                }
                
                Console.WriteLine($"[SimpleScriptService] Discovery complete. Found {scripts.Count} scripts total");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimpleScriptService] Error discovering scripts: {ex.Message}");
            }

            return scripts;
        }
    }
}
