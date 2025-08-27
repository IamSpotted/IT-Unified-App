using MauiApp1.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MauiApp1.Interfaces
{
    public interface INetopConnectService : ITransientService
    {
        Task<bool> ConnectToDeviceAsync(MauiApp1.Models.Netop device);
        bool IsNetopAvailable();
        void ConnectToDevice(string hostname);
    }
}