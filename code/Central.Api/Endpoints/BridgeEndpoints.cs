using Central.Api.Auth;
using Central.Api.Data;
using Central.Core.Enums;
using Central.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Api.Endpoints;

/// <summary>
/// The Bridge faces only these two endpoints (§15 step 3). Contract is the canonical model.
/// API-key auth is layered on in phase 6.
/// </summary>
public static class BridgeEndpoints
{
    public static void MapBridgeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        // Pull: hand pending orders to the bridge, then mark them Imported (§8).
        group.MapGet("/orders/pending", async (CentralDbContext db, string? channel, int take = 50) =>
        {
            var query = db.Orders.Where(o => o.Status == OrderStatus.Received);
            if (!string.IsNullOrWhiteSpace(channel))
            {
                query = query.Where(o => o.ChannelKey == channel);
            }

            var orders = await query
                .OrderBy(o => o.ReceivedAt)
                .Take(take)
                .ToListAsync();

            foreach (var order in orders)
            {
                order.Status = OrderStatus.Imported;
                order.ImportedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync();

            return Results.Ok(orders);
        }).AddEndpointFilter<ApiKeyEndpointFilter>();

        // Push: dispatch confirmation back from OrderWise (§8). Idempotency-Key makes retries safe.
        group.MapPost("/dispatch", async (
            CanonicalDispatch dispatch,
            HttpContext http,
            CentralDbContext db) =>
        {
            var order = await db.Orders.FindAsync(dispatch.ChannelKey, dispatch.OrderNumber);
            if (order is null)
            {
                return Results.NotFound(new { message = "Order not found." });
            }

            if (order.Status == OrderStatus.DispatchSent)
            {
                return Results.Ok(new { status = "already-sent", order.OrderNumber });
            }

            order.Status = OrderStatus.DispatchSent;
            order.DispatchedAt = DateTimeOffset.UtcNow;

            db.SyncLogs.Add(new SyncLog
            {
                ChannelKey = dispatch.ChannelKey,
                Direction = "push",
                Level = "info",
                Message = $"Dispatch {dispatch.OrderNumber} pushed " +
                          $"(key={http.Request.Headers["Idempotency-Key"].FirstOrDefault() ?? "none"})."
            });

            await db.SaveChangesAsync();

            return Results.Ok(new { status = "sent", order.OrderNumber });
        }).AddEndpointFilter<ApiKeyEndpointFilter>();
    }
}
