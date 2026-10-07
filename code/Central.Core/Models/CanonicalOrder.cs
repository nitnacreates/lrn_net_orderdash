using Central.Core.Enums;

namespace Central.Core.Models;

/// <summary>
/// The canonical order every connector maps into (§10.1).
/// Persisted as-is: PK is (ChannelKey, OrderNumber) — ASDA-style PO dedupe (§8).
/// </summary>
public class CanonicalOrder
{
    public string ChannelKey { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string? CustomerRef { get; set; }

    public DateOnly OrderDate { get; set; }
    public DateOnly? RequiredDate { get; set; }

    public CanonicalCustomer Customer { get; set; } = new();
    public List<CanonicalOrderLine> Lines { get; set; } = [];

    public OrderStatus Status { get; set; } = OrderStatus.Received;

    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ImportedAt { get; set; }
    public DateTimeOffset? DispatchedAt { get; set; }
}
