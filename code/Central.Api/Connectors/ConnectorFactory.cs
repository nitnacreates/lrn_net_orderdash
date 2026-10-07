using Central.Channels;
using Central.Core.Abstractions;
using Central.Core.Enums;
using Central.Core.Models;

namespace Central.Api.Connectors;

/// <summary>
/// Picks the connector template for a channel (§9). Adding a channel needs no change here —
/// only a new template when a whole new transport/format appears.
/// </summary>
public class ConnectorFactory(string dataRoot)
{
    public IPartnerConnector For(Channel channel) => channel.Type switch
    {
        ChannelType.FtpCsv => new FtpCsvConnector(
            Path.Combine(dataRoot, "ftp", channel.Key, "inbound"),
            Path.Combine(dataRoot, "ftp", channel.Key, "outbound")),
        ChannelType.FtpEdi => new FtpEdiConnector(
            channel.Key,
            Path.Combine(dataRoot, "edi", channel.Key, "inbound"),
            Path.Combine(dataRoot, "edi", channel.Key, "outbound")),
        ChannelType.ApiRest => new ApiRestConnector(
            channel.Key,
            Path.Combine(dataRoot, "api", channel.Key, "inbound"),
            Path.Combine(dataRoot, "api", channel.Key, "outbound")),
        ChannelType.ApiGraphQl => new ApiGraphQlConnector(
            channel.Key,
            Path.Combine(dataRoot, "graphql", channel.Key, "inbound"),
            Path.Combine(dataRoot, "graphql", channel.Key, "outbound")),
        _ => throw new NotSupportedException($"No connector template for {channel.Type}.")
    };
}
