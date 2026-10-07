using Central.Channels;
using Central.Core.Abstractions;
using Central.Core.Enums;
using Central.Core.Models;

namespace Central.Api.Connectors;

/// <summary>
/// Picks the connector template for a channel (§9). Adding a channel needs no change here —
/// only a new template when a whole new transport/format appears.
/// </summary>
public class ConnectorFactory(string ftpRoot)
{
    public IPartnerConnector For(Channel channel) => channel.Type switch
    {
        ChannelType.FtpCsv => new FtpCsvConnector(
            Path.Combine(ftpRoot, channel.Key, "inbound"),
            Path.Combine(ftpRoot, channel.Key, "outbound")),
        _ => new StubConnector(channel.Type)
    };
}
