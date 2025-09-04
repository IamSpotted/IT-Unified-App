using MauiApp1.Models;
using MauiApp1.Scripts;

namespace MauiApp1.Models
{
    /// <summary>
    /// Holds both the live (script) and database info for a device for side-by-side comparison.
    /// </summary>
    public class ComputerInfoComparisonResult
    {
        public EnhancedComputerInfo ScriptInfo { get; set; }
        public Device? DatabaseInfo { get; set; }
        public string ScanTarget { get; set; } = string.Empty; // Store the original scan input

        // Constructor
        public ComputerInfoComparisonResult(EnhancedComputerInfo scriptInfo, Device? databaseInfo, string scanTarget = "")
        {
            ScriptInfo = scriptInfo;
            DatabaseInfo = databaseInfo;
            ScanTarget = scanTarget;
        }

        // Booleans for selective updating of each field
        public bool UpdateSerialNumber { get; set; }
        public bool UpdateAssetTag { get; set; }
        public bool UpdateDomainName { get; set; }
        public bool UpdateWorkgroup { get; set; }
        public bool UpdateIsDomainJoined { get; set; }
        public bool UpdateManufacturer { get; set; }
        public bool UpdateModel { get; set; }
        public bool UpdateCpuInfo { get; set; }
        public bool UpdateTotalRamGb { get; set; }
        public bool UpdateRamType { get; set; }
        public bool UpdateStorageInfo { get; set; }
        public bool UpdateBiosVersion { get; set; }
        public bool UpdateOsName { get; set; }
        public bool UpdateOsVersion { get; set; }
        public bool UpdateOsArchitecture { get; set; }
        public bool UpdatePrimaryIp { get; set; }
        public bool UpdatePrimaryMac { get; set; }
        public bool UpdateSecondaryIps { get; set; }
        public bool UpdateSecondaryMacs { get; set; }
        public bool UpdateDnsServers { get; set; }
        public bool UpdateSubnetMasks { get; set; }
    }
}
