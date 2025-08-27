namespace MauiApp1.Interfaces
{
    /// <summary>
    /// Simple interface for scripts - based on working console app pattern
    /// </summary>
    public interface IScript
    {
        string ScriptName { get; }
        string Description { get; }
        string Category { get; }
        string Author { get; }
        void Execute();
    }
}