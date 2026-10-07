using Central.Api.Connectors;
using Central.Api.Data;
using Central.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Api.Jobs;

/// <summary>
/// The Central <-> channel clock (§5): one recurring Hangfire job per channel pulls that
/// channel's orders through its connector template and dedupes them by PO number.
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
            Level = "info",
            Message = $"Pulled {added} new order(s) from {channel.Name}."
        });

        await db.SaveChangesAsync();
    }
}
