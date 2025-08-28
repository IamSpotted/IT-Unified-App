using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.Services;

public class NetworkDeviceService : INetworkDeviceService
{
    private readonly ILogger<NetworkDeviceService> _logger;
    private readonly IDatabaseService _databaseService;

    public NetworkDeviceService(ILogger<NetworkDeviceService> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task<List<NetworkDevice>> GetNetworkDevicesAsync()
    {
        var devices = await _databaseService.GetNetworkDevicesAsync();
        var networkDevices = devices.Select(d => new NetworkDevice
        {
            Hostname = d.Hostname,
            Model = d.Model,
            PrimaryIp = d.PrimaryIp,
            Column = d.Column,
            Level = d.Level,
            Area = d.Area
            // ...other mappings...
        }).ToList();

        return networkDevices;
    }
}