using System.Threading.Tasks;
using MauiApp1.Models;

namespace MauiApp1.Interfaces
{
    public interface IAddDeviceService : ISingletonService
    {
        Task<bool> AddDeviceAsync(Models.Device device, string deviceType = "Other");
    }
}