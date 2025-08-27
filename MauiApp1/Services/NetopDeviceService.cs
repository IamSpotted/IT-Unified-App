using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.Services;

public class NetopDeviceService : INetopDeviceService
{
    private readonly ILogger<NetopDeviceService> _logger;
    private readonly IDatabaseService _databaseService;

    public NetopDeviceService(ILogger<NetopDeviceService> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task<List<Netop>> GetNetopsAsync()
    {
        var devices = await _databaseService.GetNetopsAsync();
        var netopDevices = devices.Select(d => new Netop
        {
            Hostname = d.Hostname,
            Model = d.Model,
            PrimaryIp = d.PrimaryIp,
            DeviceType = d.DeviceType,
            Area = d.Area,
            Zone = d.Zone,
            Line = d.Line,
            Pitch = d.Pitch,
            Column = d.Column,
            Level = d.Level,

            // ...other mappings...
        }).ToList();

        return netopDevices;
    }
}