using System.Threading.Tasks;

namespace MauiApp1.Interfaces
{
    public interface IRemoveDeviceService : ISingletonService
    {
        Task<bool> RemoveDeviceAsync(string hostname);
        Task<bool> RemoveDeviceAsync(string hostname, string deletionReason);
    }
}