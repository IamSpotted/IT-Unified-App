namespace MauiApp1.Interfaces;

/// <summary>
/// Service for managing networkDevices
/// </summary>
public interface INetworkDeviceService : ISingletonService
{
    Task<List<NetworkDevice>> GetNetworkDevicesAsync();
}
