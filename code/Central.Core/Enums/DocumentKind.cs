namespace Central.Core.Enums;

/// <summary>
/// The four document kinds Central exchanges with a partner (§14). Direction is implied by the kind.
/// </summary>
public enum DocumentKind
{
    Orders,        // partner -> Central -> ERP
    OrderResponse, // ERP -> Central -> partner (ack + shipment)
    Price,         // ERP -> Central -> partner
    Stock          // ERP -> Central -> partner
}
