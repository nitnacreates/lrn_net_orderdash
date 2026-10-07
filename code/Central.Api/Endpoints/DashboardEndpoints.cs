using Central.Api.Data;
using Central.Core.Enums;
using Central.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Api.Endpoints;

/// <summary>
/// Read endpoints the Next.js dashboard calls (§11). JWT-protected (§12); the bridge uses API keys.
/// </summary>
public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        // Counters (§11): new today, pending import, dispatched, errors.
        group.MapGet("/stats", async (CentralDbContext db) =>
        {
            var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
            return Results.Ok(new
            {
                newToday = await db.Orders.CountAsync(o => o.ReceivedAt >= today),
                pendingImport = await db.Orders.CountAsync(o => o.Status == OrderStatus.Received),
                imported = await db.Orders.CountAsync(o => o.Status == OrderStatus.Imported),
                dispatched = await db.Orders.CountAsync(o => o.Status == OrderStatus.DispatchSent),
                errors = await db.Orders.CountAsync(o => o.Status == OrderStatus.Error),
                channels = await db.Channels.CountAsync()
            });
        });

        // Channels list: name, type, last sync, status (§11).
        group.MapGet("/channels", async (CentralDbContext db) =>
            Results.Ok(await db.Channels.OrderBy(c => c.Name).ToListAsync()));

        // Channel detail + its orders (§11).
        group.MapGet("/channels/{key}", async (string key, CentralDbContext db) =>
        {
            var channel = await db.Channels.FindAsync(key);
            if (channel is null)
            {
                return Results.NotFound(new { message = "Channel not found." });
            }

            var orders = await db.Orders
                .Where(o => o.ChannelKey == key)
                .OrderByDescending(o => o.ReceivedAt)
                .Take(100)
                .ToListAsync();

            return Results.Ok(new { channel, orders });
        });

        // Orders view: filter by channel, status, date (§11).
        group.MapGet("/orders", async (
            CentralDbContext db, string? channel, OrderStatus? status, DateOnly? from, DateOnly? to) =>
        {
            var query = db.Orders.AsQueryable();
            if (!string.IsNullOrWhiteSpace(channel))
            {
                query = query.Where(o => o.ChannelKey == channel);
            }

            if (status is not null)
            {
                query = query.Where(o => o.Status == status);
            }

            if (from is not null)
            {
                query = query.Where(o => o.OrderDate >= from);
            }

            if (to is not null)
            {
                query = query.Where(o => o.OrderDate <= to);
            }

            return Results.Ok(await query.OrderByDescending(o => o.ReceivedAt).Take(200).ToListAsync());
        });

        // Logs / errors (§11).
        group.MapGet("/logs", async (CentralDbContext db, string? channel, string? level, int take = 100) =>
        {
            var query = db.SyncLogs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(channel))
            {
                query = query.Where(l => l.ChannelKey == channel);
            }

            if (!string.IsNullOrWhiteSpace(level))
            {
                query = query.Where(l => l.Level == level);
            }

            return Results.Ok(await query.OrderByDescending(l => l.CreatedAt).Take(take).ToListAsync());
        });

        // Replay a failed order (§11): put it back to Received so the bridge picks it up again.
        group.MapPost("/orders/{channelKey}/{orderNumber}/replay", async (
            string channelKey, string orderNumber, CentralDbContext db) =>
        {
            var order = await db.Orders.FindAsync(channelKey, orderNumber);
            if (order is null)
            {
                return Results.NotFound(new { message = "Order not found." });
            }

            order.Status = OrderStatus.Received;
            order.ImportedAt = null;
            db.SyncLogs.Add(new SyncLog
            {
                ChannelKey = channelKey,
                Direction = "replay",
                Level = "info",
                Message = $"Order {orderNumber} replayed."
            });
            await db.SaveChangesAsync();

            return Results.Ok(new { status = "replayed", orderNumber });
        });
    }
}
