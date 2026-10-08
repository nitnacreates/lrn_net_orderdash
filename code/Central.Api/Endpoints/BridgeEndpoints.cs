using Central.Api.Auth;
using Central.Api.Data;
using Central.Api.Jobs;
using Central.Core.Enums;
using Central.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Api.Endpoints;

/// <summary>
/// The Bridge faces these endpoints (§15 step 3). Contract is the canonical model.
/// API-key auth is layered on in phase 6. From phase 11 the bridge exchanges the four
/// document kinds (§14): it pulls orders, and pushes responses / price / stock back.
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

        // Push: acknowledgement + shipment update back from OrderWise (§10.4, §14).
        // Idempotency-Key makes retries safe; the response is stored and relayed to the partner.
        group.MapPost("/dispatch", async (
            OrderResponse response,
            HttpContext http,
            CentralDbContext db,
            DocumentRelay relay) =>
        {
            var order = await db.Orders.FindAsync(response.ChannelKey, response.OrderNumber);
            if (order is null)
            {
                return Results.NotFound(new { message = "Order not found." });
            }

            if (order.Status == OrderStatus.DispatchSent)
            {
                return Results.Ok(new { status = "already-sent", order.OrderNumber });
            }

            db.OrderResponses.Add(response);

            order.Status = response.Status == OrderResponseStatus.Rejected
                ? OrderStatus.Error
                : OrderStatus.DispatchSent;
            order.DispatchedAt = DateTimeOffset.UtcNow;

            db.SyncLogs.Add(new SyncLog
            {
                ChannelKey = response.ChannelKey,
                Direction = "push",
                Document = DocumentKind.OrderResponse.ToString(),
                Level = response.Status == OrderResponseStatus.Rejected ? "warn" : "info",
                Message = $"Response {response.Status} for {response.OrderNumber} " +
                          $"(key={http.Request.Headers["Idempotency-Key"].FirstOrDefault() ?? "none"})."
            });

            await db.SaveChangesAsync();
            await relay.PushAsync(DocumentKind.OrderResponse, response, response.ChannelKey);

            return Results.Ok(new { status = "sent", order.OrderNumber });
        }).AddEndpointFilter<ApiKeyEndpointFilter>();

        // Ingest: the ERP is the master for price (§14). Store, then relay to the partner.
        group.MapPost("/price", async (PriceList price, CentralDbContext db, DocumentRelay relay) =>
        {
            var existing = await db.PriceLists
                .Where(p => p.ChannelKey == price.ChannelKey)
                .ToListAsync();
            db.PriceLists.RemoveRange(existing);

            price.Id = 0;
            price.UpdatedAt = DateTimeOffset.UtcNow;
            db.PriceLists.Add(price);

            db.SyncLogs.Add(new SyncLog
            {
                ChannelKey = price.ChannelKey,
                Direction = "in",
                Document = DocumentKind.Price.ToString(),
                Level = "info",
                Message = $"Received price list ({price.Items.Count} item(s))."
            });

            await db.SaveChangesAsync();
            var sent = await relay.PushAsync(DocumentKind.Price, price, price.ChannelKey);

            return Results.Ok(new { status = "accepted", items = price.Items.Count, relayedTo = sent });
        }).AddEndpointFilter<ApiKeyEndpointFilter>();

        // Ingest: the ERP is the master for stock (§14). Upsert per channel, then relay.
        group.MapPost("/stock", async (List<StockLevel> levels, CentralDbContext db, DocumentRelay relay) =>
        {
            foreach (var level in levels)
            {
                var existing = await db.StockLevels.FindAsync(level.ChannelKey, level.Sku, level.Warehouse);
                if (existing is null)
                {
                    db.StockLevels.Add(level);
                }
                else
                {
                    existing.QtyOnHand = level.QtyOnHand;
                    existing.QtyAvailable = level.QtyAvailable;
                    existing.AsOf = level.AsOf;
                }
            }

            await db.SaveChangesAsync();

            var relayed = 0;
            foreach (var batch in levels.GroupBy(l => l.ChannelKey))
            {
                db.SyncLogs.Add(new SyncLog
                {
                    ChannelKey = batch.Key,
                    Direction = "in",
                    Document = DocumentKind.Stock.ToString(),
                    Level = "info",
                    Message = $"Received {batch.Count()} stock level(s)."
                });

                relayed += await relay.PushAsync(DocumentKind.Stock, batch.ToList(), batch.Key);
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { status = "accepted", levels = levels.Count, relayedTo = relayed });
        }).AddEndpointFilter<ApiKeyEndpointFilter>();
    }
}
