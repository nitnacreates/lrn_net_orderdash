namespace Central.Core.Models;

/// <summary>Per-channel sync trail shown on the dashboard (§11).</summary>
public class SyncLog
{
    public int Id { get; set; }
    public string ChannelKey { get; set; } = string.Empty;
    public string Direction { get; set; } = "pull";
    public string Level { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
