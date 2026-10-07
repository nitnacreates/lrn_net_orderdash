using Central.Core.Enums;

namespace Central.Core.Models;

/// <summary>A channel Central talks to (§3, §9). Key is the natural PK, e.g. "asda".</summary>
public class Channel
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ChannelType Type { get; set; }
    public int ScheduleMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastSyncAt { get; set; }

    /// <summary>green / amber / red for the dashboard (§11).</summary>
    public string Status { get; set; } = "unknown";
}
