namespace Central.Core.Enums;

/// <summary>Lifecycle of an order as it moves through Central (§10.1).</summary>
public enum OrderStatus
{
    Received,
    Imported,
    DispatchSent,
    Error
}
