namespace MauiApp1.Interfaces;

/// <summary>
/// Service for managing cameras
/// </summary>
public interface INetopDeviceService : ISingletonService
{
    Task<List<Netop>> GetNetopsAsync();
}
