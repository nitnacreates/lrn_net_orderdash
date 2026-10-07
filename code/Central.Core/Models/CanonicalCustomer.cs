namespace Central.Core.Models;

/// <summary>Customer block of a canonical order (§10.1).</summary>
public class CanonicalCustomer
{
    public string Name { get; set; } = string.Empty;
    public CanonicalAddress DeliveryAddress { get; set; } = new();
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
