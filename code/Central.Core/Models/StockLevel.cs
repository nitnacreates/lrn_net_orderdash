namespace Central.Core.Models;

/// <summary>Stock on hand for a SKU/warehouse, pushed out to channels (§10.6). ERP is the master (§14).</summary>
public class StockLevel
{
    public string ChannelKey { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public int QtyOnHand { get; set; }
    public int QtyAvailable { get; set; }
    public DateTimeOffset AsOf { get; set; } = DateTimeOffset.UtcNow;
}
