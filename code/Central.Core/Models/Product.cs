namespace Central.Core.Models;

/// <summary>Bedding product; SKU is the natural PK and maps to OrderWise Code / eCommerceCode (§10.3, §13).</summary>
public class Product
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
