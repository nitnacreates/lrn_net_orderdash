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
    /// <summary>XSLT maps ship next to the assembly (§16); copied from Central.Channels/Xslt.</summary>
    private readonly string _xsltRoot = Path.Combine(AppContext.BaseDirectory, "Xslt");

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
        // FTP + XML clients get the three-folder contract (§15) and XSLT maps (§16).
        ChannelType.FtpXml => new FtpXmlConnector(
            channel.Key,
            Path.Combine(dataRoot, "xml", channel.Key, "orders"),
            Path.Combine(dataRoot, "xml", channel.Key, "price"),
            Path.Combine(dataRoot, "xml", channel.Key, "stock"),
            Path.Combine(dataRoot, "xml", channel.Key, "responses"),
            _xsltRoot),
        _ => throw new NotSupportedException($"No connector template for {channel.Type}.")
    };
}
