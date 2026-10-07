using Central.Core.Abstractions;
using Central.Core.Enums;
using Central.Core.Models;

namespace Central.Channels;

/// <summary>
/// Placeholder for the templates not built yet (FtpEdi, ApiRest, ApiGraphQl — §15 step 9).
/// Keeps seeded channels of those types running without pretending to do real work.
/// </summary>
public class StubConnector(ChannelType type) : IPartnerConnector
{
    public ChannelType Type { get; } = type;

    public Task<IReadOnlyList<CanonicalOrder>> PullOrdersAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CanonicalOrder>>([]);

    public Task PushDispatchAsync(CanonicalDispatch dispatch, CancellationToken ct = default) =>
        Task.CompletedTask;
}
