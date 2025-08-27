namespace MauiApp1.Interfaces;

/// <summary>
/// Service for managing cameras
/// </summary>
public interface ICameraService : ISingletonService
{
    Task<List<Camera>> GetCamerasAsync();
}
