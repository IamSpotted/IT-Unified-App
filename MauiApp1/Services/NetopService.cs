using MauiApp1.Interfaces;
using MauiApp1.Models;
using System.Diagnostics;

namespace MauiApp1.Services;

public class NetopService : INetopService
{
    private readonly ILogger<NetopService> _logger;
    private readonly IDatabaseService _databaseService;
    private readonly ISettingsService _settingsService;

    public NetopService(ILogger<NetopService> logger, IDatabaseService databaseService, ISettingsService settingsService)
    {
        _logger = logger;
        _databaseService = databaseService;
        _settingsService = settingsService;
    }

    public async Task<List<Netop>> GetNetopsAsync()
    {
        var devices = await _databaseService.GetNetopsAsync();
        var netops = devices.Select(d => new Netop
        {
            Hostname = d.Hostname,
            Model = d.Model,
            PrimaryIp = d.PrimaryIp,
            Column = d.Column,
            Level = d.Level,
            Area = d.Area
            // ...other mappings...
        }).ToList();

        return netops;
    }

    public void ConnectToDevice(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname)) throw new ArgumentNullException(nameof(hostname));

        var netopPath = _settingsService.GetAsync<string>("NetopServicePath", string.Empty).GetAwaiter().GetResult();
        var processStartInfo = new ProcessStartInfo
        {
            FileName = netopPath,
            Arguments = $"/C:TCP/IP /H:{hostname}",
            UseShellExecute = true
        };

        try
        {
            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start Netop connection: {ex.Message}");
        }
    }
}