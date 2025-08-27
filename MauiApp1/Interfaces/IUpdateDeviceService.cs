using System.Threading.Tasks;
using MauiApp1.Models;

namespace MauiApp1.Interfaces
{
    public interface IUpdateDeviceService : ISingletonService
    {
        Task<bool> UpdateDeviceAsync(Models.Device device, string deviceType = "Other");
    }
}