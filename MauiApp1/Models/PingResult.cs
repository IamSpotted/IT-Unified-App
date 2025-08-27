namespace MauiApp1.Models;

/// <summary>
/// Represents the result of a single ping operation.
/// Used by connectivity testing functionality across ViewModels.
/// </summary>
public class PingResult
{
    /// <summary>
    /// Sequential number of this ping in the current test sequence.
    /// </summary>
    public int PingNumber { get; set; }
    
    /// <summary>
    /// Status of the ping operation (Success, Failed, Error, etc.).
    /// </summary>
    public string Status { get; set; } = "";
    
    /// <summary>
    /// Color associated with the status for UI display.
    /// </summary>
    public Color StatusColor { get; set; } = Colors.Black;
    
    /// <summary>
    /// Response time or error indication for the ping.
    /// </summary>
    public string Time { get; set; } = "";
    
    /// <summary>
    /// Timestamp when this ping operation occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
