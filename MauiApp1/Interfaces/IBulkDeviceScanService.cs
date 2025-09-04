using MauiApp1.Models;

namespace MauiApp1.Interfaces
{
    /// <summary>
    /// Interface for bulk device scanning operations from text file input
    /// </summary>
    public interface IBulkDeviceScanService
    {
        /// <summary>
        /// Processes a text file containing hostnames/IP addresses and adds devices to database
        /// </summary>
        /// <param name="filePath">Path to the text file containing hostnames/IPs (one per line)</param>
        /// <param name="applicationUser">User performing the bulk scan</param>
        /// <param name="discoverySessionId">Discovery session ID for audit tracking</param>
        /// <param name="changeReason">Reason for adding these devices</param>
        /// <returns>Bulk scan result with statistics</returns>
        Task<BulkScanResult> ProcessBulkScanAsync(string filePath, string applicationUser, Guid discoverySessionId, string changeReason);

        /// <summary>
        /// Processes a list of hostnames/IP addresses and adds devices to database
        /// </summary>
        /// <param name="hostnames">List of hostnames/IP addresses to scan</param>
        /// <param name="applicationUser">User performing the bulk scan</param>
        /// <param name="discoverySessionId">Discovery session ID for audit tracking</param>
        /// <param name="changeReason">Reason for adding these devices</param>
        /// <param name="originalFilePath">Optional path to original file for failure reporting</param>
        /// <returns>Bulk scan result with statistics</returns>
        Task<BulkScanResult> ProcessBulkScanAsync(IEnumerable<string> hostnames, string applicationUser, Guid discoverySessionId, string changeReason, string? originalFilePath = null);
    }
}
