using Central.Core.Enums;

namespace Central.Core.Models;

/// <summary>
/// ERP -> Central -> partner acknowledgement + shipment update (§10.4). Replaces the thin
/// CanonicalDispatch — an ack and a shipment are two moments of the same response.
/// </summary>
public class OrderResponse
{
    public int Id { get; set; }
    public string ChannelKey { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;

    public OrderResponseStatus Status { get; set; } = OrderResponseStatus.Acknowledged;
    public string? Reason { get; set; }

    public DateOnly? DispatchedDate { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
