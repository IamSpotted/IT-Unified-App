using MauiApp1.Models;

namespace MauiApp1.Models
{
    /// <summary>
    /// Holds both the live (script) and database info for a device for side-by-side comparison.
    /// </summary>
    public class ComputerInfoComparisonResult
    {

        public ComputerInfo ScriptInfo { get; set; }
        public Device ScriptedDevice => ToDevice(ScriptInfo)!;
        public Device? DatabaseInfo { get; set; }

        // Constructor
        public ComputerInfoComparisonResult(ComputerInfo scriptInfo, Device? databaseInfo)
        {
            ScriptInfo = scriptInfo;
            DatabaseInfo = databaseInfo;
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
        public bool UpdateDefaultGateways { get; set; }
        public bool UpdateSubnetMasks { get; set; }

        // Optionally, convert ComputerInfo to Device for easier comparison
        private Device ToDevice(ComputerInfo info)
        {
            // Get primary IP and MAC from first adapter with data
            var primaryAdapter = info.NetworkAdapters?.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.IPAddress) || !string.IsNullOrWhiteSpace(a.MACAddress));
            string? primaryIp = primaryAdapter?.IPAddress;
            string? primaryMac = primaryAdapter?.MACAddress;
            return new Device
            {
                Hostname = info.ComputerName,
                Manufacturer = info.Manufacturer,
                Model = info.Model,
                SerialNumber = info.SerialNumber,
                AssetTag = info.AssetTag,
                IpAddress = primaryIp,
                MacAddress = primaryMac,
                // Add more mappings as needed
            };
        }
    }
}
