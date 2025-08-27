using MauiApp1.Interfaces;
using MauiApp1.Models;

namespace MauiApp1.Services;

public class CameraService : ICameraService
{
    private readonly ILogger<CameraService> _logger;
    private readonly IDatabaseService _databaseService;

    public CameraService(ILogger<CameraService> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task<List<Camera>> GetCamerasAsync()
    {
        var devices = await _databaseService.GetCamerasAsync();
        var cameras = devices.Select(d => new Camera
        {
            Hostname = d.Hostname,
            Model = d.Model,
            PrimaryIp = d.PrimaryIp,
            Column = d.Column,
            Level = d.Level,
            Area = d.Area
            // ...other mappings...
        }).ToList();

        return cameras;
    }
}