namespace Central.Core.Models;

/// <summary>Dispatch confirmation pushed from OrderWise back to a channel (§8, §10).</summary>
public class CanonicalDispatch
{
    public string ChannelKey { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public DateOnly DispatchedDate { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
}
