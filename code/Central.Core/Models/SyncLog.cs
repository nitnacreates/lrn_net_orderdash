namespace Central.Core.Models;

/// <summary>Per-channel sync trail shown on the dashboard (§11).</summary>
public class SyncLog
{
    public int Id { get; set; }
    public string ChannelKey { get; set; } = string.Empty;
    public string Direction { get; set; } = "pull";

    /// <summary>Which document kind this entry is about (§14): orders / OrderResponse / Price / Stock.</summary>
    public string Document { get; set; } = "orders";

    public string Level { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
