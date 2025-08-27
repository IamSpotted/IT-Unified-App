namespace MauiApp1.Interfaces;

/// <summary>
/// Service for managing cameras
/// </summary>
public interface INetopService : ISingletonService
{
    Task<List<Netop>> GetNetopsAsync();
    void ConnectToDevice(string hostname);
}
