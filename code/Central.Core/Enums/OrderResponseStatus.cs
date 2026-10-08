namespace Central.Core.Enums;

/// <summary>Acknowledgement outcome for an order (§10.4).</summary>
public enum OrderResponseStatus
{
    Acknowledged,
    Accepted,
    Rejected
}
