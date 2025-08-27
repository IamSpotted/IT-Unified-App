namespace MauiApp1.Services;

/// <summary>
/// Service for managing printers
/// </summary>
public class PrinterService : IPrinterService
{
    private readonly ILogger<PrinterService> _logger;
    private readonly IDatabaseService _databaseService;

    public PrinterService(ILogger<PrinterService> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task<List<Printer>> GetPrintersAsync()
    {
        var devices = await _databaseService.GetPrintersAsync();
        var printers = devices.Select(d => new Printer
        {
            Hostname = d.Hostname,
            PrimaryIp = d.PrimaryIp,
            Area = d.Area,
            Zone = d.Zone,
            Line = d.Line,
            Pitch = d.Pitch,
            Column = d.Column,
            Level = d.Level,
            Model = d.Model,
            SerialNumber = d.SerialNumber
            // ...other mappings...
        }).ToList();

        return printers;
    }
}
