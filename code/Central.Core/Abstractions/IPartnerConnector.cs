using Central.Core.Enums;
using Central.Core.Models;

namespace Central.Core.Abstractions;

/// <summary>
/// The contract every channel template implements (§9, §14). Lives in Central, never in OrderWise.
/// Document-oriented: a channel declares the kinds it exchanges, then pulls orders / pushes by kind.
/// Five implementations behind it: FtpCsv, FtpEdi, ApiRest, ApiGraphQl, FtpXml.
/// </summary>
public interface IPartnerConnector
{
    ChannelType Type { get; }

    /// <summary>Which document kinds this channel exchanges (§14).</summary>
    IReadOnlySet<DocumentKind> Supported { get; }

    /// <summary>Inbound: partner -> Central.</summary>
    Task<IReadOnlyList<CanonicalOrder>> PullOrdersAsync(CancellationToken ct = default);

    /// <summary>
    /// Outbound: Central -> partner. <paramref name="document"/> is one of
    /// <see cref="OrderResponse"/>, <see cref="PriceList"/> or <see cref="StockLevel"/> (§14).
    /// </summary>
    Task PushAsync(DocumentKind kind, object document, CancellationToken ct = default);
}
