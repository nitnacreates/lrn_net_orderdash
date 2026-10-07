using Central.Core.Enums;
using Central.Core.Models;

namespace Central.Core.Abstractions;

/// <summary>
/// The contract every channel template implements (§9). Lives in Central, never in OrderWise.
/// Four implementations behind it: FtpCsv, FtpEdi, ApiRest, ApiGraphQl.
/// </summary>
public interface IPartnerConnector
{
    ChannelType Type { get; }

    Task<IReadOnlyList<CanonicalOrder>> PullOrdersAsync(CancellationToken ct = default);
    Task PushDispatchAsync(CanonicalDispatch dispatch, CancellationToken ct = default);
}
