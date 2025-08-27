namespace MauiApp1.Interfaces;

/// <summary>
/// Interface for printer service following Single Responsibility Principle
/// </summary>
public interface IPrinterService : ISingletonService
{
    /// <summary>
    /// Gets all printers from the data source
    /// </summary>
    Task<List<Printer>> GetPrintersAsync();
}
