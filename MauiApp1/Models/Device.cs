using Windows.Networking;

namespace MauiApp1.Models;

/// <summary>
/// Example model showing how to implement IFilterable for any entity
/// </summary>
public class Device : IFilterable
{
    // --- Added for ComputerInfo/Inventory fields ---
    public int device_id { get; set; } // Unique identifier for the device
    public string DeviceStatus { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Other";
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }
    public string? PrimaryIp { get; set; }
    public string? PrimaryMac { get; set; }
    public string? DomainName { get; set; }
    public string? Workgroup { get; set; }
    public bool? IsDomainJoined { get; set; }
    public string? Manufacturer { get; set; }
    public string? CpuInfo { get; set; }
    public decimal TotalRamGb { get; set; }
    public string? RamType { get; set; }
    public string? StorageInfo { get; set; }
    public string? BiosVersion { get; set; }
    public string? OsName { get; set; }
    public string? OSVersion { get; set; }
    public string? OsArchitecture { get; set; }
    public string? SecondaryIps { get; set; }
    public string? SecondaryMacs { get; set; }
    public string? DnsServers { get; set; }
    public string? DefaultGateways { get; set; }
    public string? SubnetMasks { get; set; }
    public string? DiscoveryMethod { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastDiscovered { get; set; }
    public string? device_type { get; set; }
    public string? device_status { get; set; }
    public string? Floor { get; set; }
    public string? Pillar { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    
    // Additional filter properties to match cameras
    public string Area { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
    public string Column { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Pitch { get; set; } = string.Empty;
    

    // Extended database properties from devices table


    public string? MacAddress { get; set; }
    public string? Model { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? ServiceDate { get; set; }
    public DateTime? WarrantyDate { get; set; }
    public string? AdditionalNotes { get; set; }
    public string? WebLink { get; set; }
    public string? WebLinkName { get; set; }
    public string? WebInterfaceUrl { get; set; }
    public string? EquipmentGroup { get; set; }

    // Additional devices table properties
    public string? IpAddress { get; set; }
    
    // Computed properties
    public string FullLocation => string.IsNullOrEmpty(Area) && string.IsNullOrEmpty(Zone) && string.IsNullOrEmpty(Line) 
        ? "No Location Set" 
        : $"{Area} - {Zone} - {Line}".Trim(' ', '-');
    
    public string GetFilterValue(string filterProperty)
    {
        return filterProperty.ToLowerInvariant() switch
        {
            "type" => device_type ?? string.Empty,
            "department" => Department ?? string.Empty,
            "location" => Location ?? string.Empty,
            "area" => Area ?? string.Empty,
            "zone" => Zone ?? string.Empty,
            "line" => Line ?? string.Empty,
            "column" => Pillar ?? string.Empty,
            "level" => Floor ?? string.Empty,
            "pitch" => Pitch ?? string.Empty,
            "name" => Hostname ?? string.Empty,
            "hostname" => Hostname ?? string.Empty,
            "manufacturer" => Manufacturer ?? string.Empty,
            "model" => Model ?? string.Empty,
            "serialnumber" => SerialNumber ?? string.Empty,
            "assettag" => AssetTag ?? string.Empty,
            _ => string.Empty
        };
    }

    public bool MatchesFilter(string filterProperty, string filterValue)
    {
        if (string.IsNullOrEmpty(filterValue))
            return true;

        var actualValue = GetFilterValue(filterProperty);
        
        return filterProperty.ToLowerInvariant() switch
        {
            _ => string.Equals(actualValue, filterValue, StringComparison.OrdinalIgnoreCase)
        };
    }
}
