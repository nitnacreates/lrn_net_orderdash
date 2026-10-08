namespace Central.Core.Enums;

/// <summary>The five channel templates a connector can implement (§9).</summary>
public enum ChannelType
{
    FtpCsv,
    FtpEdi,
    ApiRest,
    ApiGraphQl,
    FtpXml
}
