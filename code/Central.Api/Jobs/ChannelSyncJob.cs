using Central.Api.Connectors;
using Central.Api.Data;
using Central.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Api.Jobs;

/// <summary>
/// The Central &lt;-&gt; channel clock (§5): one recurring Hangfire job per channel pulls that
/// channel's orders through its connector template and dedupes them by PO number.
/// Price/stock/responses are relayed on ingest instead of polled (§14).
/// </summary>
public class ChannelSyncJob(CentralDbContext db, ConnectorFactory connectors)
{
    public async Task RunAsync(string channelKey)
    {
        var channel = await db.Channels.FindAsync(channelKey);
        if (channel is null || !channel.IsActive)
        {
            return;
        }

        var connector = connectors.For(channel);
        if (!connector.Supported.Contains(Core.Enums.DocumentKind.Orders))
        {
            return;
        }

        var orders = await connectors.For(channel).PullOrdersAsync();

        var added = 0;
        foreach (var order in orders)
        {
            var exists = await db.Orders.AnyAsync(o =>
                o.ChannelKey == order.ChannelKey && o.OrderNumber == order.OrderNumber);
            if (exists)
            {
                continue;
            }

            db.Orders.Add(order);
            added++;
        }

        channel.LastSyncAt = DateTimeOffset.UtcNow;
        db.SyncLogs.Add(new SyncLog
        {
            ChannelKey = channelKey,
            Direction = "pull",
            Document = "orders",
            Level = "info",
            Message = $"Pulled {added} new order(s) from {channel.Name}."
        });

        await db.SaveChangesAsync();
    }
}
