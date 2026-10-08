namespace Central.Core.Models;

/// <summary>Prices for a channel, effective over a window (§10.5). ERP is the master (§14).</summary>
public class PriceList
{
    public int Id { get; set; }
    public string ChannelKey { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public List<PriceItem> Items { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PriceItem
{
    public string Sku { get; set; } = string.Empty;
    public string Currency { get; set; } = "GBP";
    public decimal UnitPrice { get; set; }
}
