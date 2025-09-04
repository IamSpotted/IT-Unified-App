namespace MauiApp1.Models
{
    /// <summary>
    /// Result model for bulk device scanning operations
    /// </summary>
    public class BulkScanResult
    {
        /// <summary>
        /// Total number of hostnames/IPs processed
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Number of devices successfully added to database
        /// </summary>
        public int SuccessfullyAdded { get; set; }

        /// <summary>
        /// Number of devices that failed to scan or add
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Number of devices that were skipped (duplicates, etc.)
        /// </summary>
        public int Skipped { get; set; }

        /// <summary>
        /// List of detailed results for each processed device
        /// </summary>
        public List<BulkScanDeviceResult> DeviceResults { get; set; } = new List<BulkScanDeviceResult>();

        /// <summary>
        /// Overall success of the bulk operation
        /// </summary>
        public bool OverallSuccess => Failed == 0 && TotalProcessed > 0;

        /// <summary>
        /// Start time of the bulk scan operation
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the bulk scan operation
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total duration of the bulk scan operation
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Path to the failure report file (if any failures occurred)
        /// </summary>
        public string? FailureReportPath { get; set; }

        /// <summary>
        /// Summary message of the bulk scan results
        /// </summary>
        public string Summary => $"Processed {TotalProcessed} devices: {SuccessfullyAdded} added, {Failed} failed, {Skipped} skipped. Duration: {Duration:mm\\:ss}";
    }

    /// <summary>
    /// Result model for individual device scanning within bulk operation
    /// </summary>
    public class BulkScanDeviceResult
    {
        /// <summary>
        /// Hostname or IP address that was scanned
        /// </summary>
        public string HostnameOrIP { get; set; } = string.Empty;

        /// <summary>
        /// Whether the scan and add operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Status message for this device
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Resolved computer name if different from input
        /// </summary>
        public string? ResolvedComputerName { get; set; }

        /// <summary>
        /// Device ID if successfully added to database
        /// </summary>
        public int? DeviceId { get; set; }

        /// <summary>
        /// Time taken to process this device
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Action taken for this device
        /// </summary>
        public BulkScanDeviceAction Action { get; set; }
    }

    /// <summary>
    /// Actions that can be taken for a device during bulk scanning
    /// </summary>
    public enum BulkScanDeviceAction
    {
        Added,
        Failed,
        Skipped,
        AlreadyExists
    }
}
