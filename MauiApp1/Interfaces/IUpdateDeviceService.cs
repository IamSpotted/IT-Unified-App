using System.Threading.Tasks;
using MauiApp1.Models;

namespace MauiApp1.Interfaces
{
    public interface IUpdateDeviceService : ISingletonService
    {
        Task<bool> UpdateDeviceAsync(Models.Device device, string deviceType = "Other");
        
        /// <summary>
        /// Updates a device with audit information including application user, discovery session ID, and change reason
        /// </summary>
        Task<bool> UpdateDeviceAsync(Models.Device device, string applicationUser, string discoverySessionId, string changeReason, string deviceType = "Other");
    }
}