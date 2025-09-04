using System.Threading.Tasks;
using MauiApp1.Models;

namespace MauiApp1.Interfaces
{
    public interface IAddDeviceService : ISingletonService
    {
        Task<bool> AddDeviceAsync(Models.Device device, string deviceType = "Other");
        Task<bool> AddDeviceAsync(Models.Device device, string deviceType, string applicationUser, Guid? discoverySessionId, string changeReason);
        
        /// <summary>
        /// Adds a device with duplicate checking and resolution options
        /// </summary>
        /// <param name="device">The device to add</param>
        /// <param name="deviceType">The type of device</param>
        /// <param name="checkDuplicates">Whether to check for duplicates before adding</param>
        /// <param name="resolutionOptions">Options for resolving duplicates if found</param>
        /// <returns>Result indicating success and any duplicate handling performed</returns>
        Task<DeviceAddResult> AddDeviceWithDuplicateCheckAsync(Models.Device device, string deviceType = "Other", bool checkDuplicates = true, DuplicateResolutionOptions? resolutionOptions = null);
    }
    
    /// <summary>
    /// Result of adding a device with duplicate checking
    /// </summary>
    public class DeviceAddResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Human-readable message about the operation result
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether duplicates were found during the check
        /// </summary>
        public bool DuplicatesFound { get; set; }
        
        /// <summary>
        /// The duplicate detection result if duplicates were found
        /// </summary>
        public DuplicateDetectionResult? DuplicateDetectionResult { get; set; }
        
        /// <summary>
        /// Action that was taken (Added, Updated, Merged, Cancelled)
        /// </summary>
        public DeviceAddAction ActionTaken { get; set; }
        
        /// <summary>
        /// The device ID of the added/updated device
        /// </summary>
        public int? DeviceId { get; set; }
        
        /// <summary>
        /// Any error details if the operation failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }
    
    /// <summary>
    /// Action taken when adding a device
    /// </summary>
    public enum DeviceAddAction
    {
        None,
        Added,
        Updated,
        Merged,
        Cancelled,
        Failed
    }
}