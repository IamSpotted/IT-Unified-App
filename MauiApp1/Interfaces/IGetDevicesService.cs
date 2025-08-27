using System.Collections.Generic;
using System.Threading.Tasks;
using MauiApp1.Models;

namespace MauiApp1.Interfaces
{
    public interface IGetDevicesService : ISingletonService
    {
        Task<List<MauiApp1.Models.Device>> GetAllDevicesAsync();
    }
}