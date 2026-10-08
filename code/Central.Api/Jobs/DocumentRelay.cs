using Central.Api.Connectors;
using Central.Api.Data;
using Central.Core.Enums;
using Central.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Api.Jobs;

/// <summary>
/// Fans a document out to the channels that exchange that kind (§14). Used for the outbound
/// half of the exchange: an order response goes back to its own channel; price and stock go to
/// every channel that advertises them.
/// </summary>
public class DocumentRelay(CentralDbContext db, ConnectorFactory connectors)
{
    public async Task<int> PushAsync(DocumentKind kind, object document, string? onlyChannel = null)
    {
        var channels = await db.Channels.Where(c => c.IsActive).ToListAsync();

        var sent = 0;
        foreach (var channel in channels)
        {
            if (onlyChannel is not null && channel.Key != onlyChannel)
            {
                continue;
            }

            var connector = connectors.For(channel);
            if (!connector.Supported.Contains(kind))
            {
                continue;
            }

            await connector.PushAsync(kind, document);
            sent++;

            db.SyncLogs.Add(new SyncLog
            {
                ChannelKey = channel.Key,
                Direction = "push",
                Document = kind.ToString(),
                Level = "info",
                Message = $"Pushed {kind} to {channel.Name}."
            });
        }

        await db.SaveChangesAsync();
        return sent;
    }
}
