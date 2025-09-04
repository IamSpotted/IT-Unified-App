using MauiApp1.Models;

namespace MauiApp1.Models
{
    /// <summary>
    /// Result of device duplicate detection with details about potential matches
    /// </summary>
    public class DuplicateDetectionResult
    {
        /// <summary>
        /// Whether any potential duplicates were found
        /// </summary>
        public bool HasDuplicates { get; set; }

        /// <summary>
        /// List of existing devices that match the new device criteria
        /// </summary>
        public List<Device> PotentialDuplicates { get; set; } = new List<Device>();

        /// <summary>
        /// Details about what fields matched for each duplicate
        /// </summary>
        public List<DuplicateMatchDetail> MatchDetails { get; set; } = new List<DuplicateMatchDetail>();

        /// <summary>
        /// The device being checked for duplicates
        /// </summary>
        public Device? NewDevice { get; set; }
    }

    /// <summary>
    /// Details about how a potential duplicate was matched
    /// </summary>
    public class DuplicateMatchDetail
    {
        /// <summary>
        /// The existing device that matched
        /// </summary>
        public Device ExistingDevice { get; set; } = new Device();

        /// <summary>
        /// Fields that matched between new and existing device
        /// </summary>
        public List<string> MatchedFields { get; set; } = new List<string>();

        /// <summary>
        /// Match confidence level (High, Medium, Low)
        /// </summary>
        public DuplicateMatchConfidence MatchConfidence { get; set; }

        /// <summary>
        /// Specific reason for the match
        /// </summary>
        public string MatchReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Confidence level for duplicate matches
    /// </summary>
    public enum DuplicateMatchConfidence
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Action to take when duplicates are detected
    /// </summary>
    public enum DuplicateResolutionAction
    {
        Cancel,
        CreateNew,
        UpdateExisting,
        MergeData
    }

    /// <summary>
    /// Options for resolving duplicate device conflicts
    /// </summary>
    public class DuplicateResolutionOptions
    {
        /// <summary>
        /// Action chosen by the user
        /// </summary>
        public DuplicateResolutionAction Action { get; set; }

        /// <summary>
        /// The existing device to update (if applicable)
        /// </summary>
        public Device? SelectedExistingDevice { get; set; }

        /// <summary>
        /// Fields to merge/update from the new device data
        /// </summary>
        public List<string> FieldsToMerge { get; set; } = new List<string>();

        /// <summary>
        /// User-provided reason for the resolution
        /// </summary>
        public string? ResolutionReason { get; set; }

        /// <summary>
        /// Whether to preserve existing data when merging
        /// </summary>
        public bool PreserveExistingData { get; set; } = true;
    }
}
