namespace Central.Core.Models;

/// <summary>One order line in canonical form (§10.1).</summary>
public class CanonicalOrderLine
{
    public string Sku { get; set; } = string.Empty;
    public string? LineRef { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? TaxCode { get; set; }
}
