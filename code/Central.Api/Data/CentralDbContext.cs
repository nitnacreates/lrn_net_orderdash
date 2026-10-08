using Central.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Api.Data;

/// <summary>
/// Persists the canonical model directly — no separate entity/DTO mapping layer (§6).
/// </summary>
public class CentralDbContext(DbContextOptions<CentralDbContext> options) : DbContext(options)
{
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<CanonicalOrder> Orders => Set<CanonicalOrder>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    // Document exchange (§14): responses back to partners, plus price and stock out.
    public DbSet<OrderResponse> OrderResponses => Set<OrderResponse>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Channel>(e =>
        {
            e.HasKey(c => c.Key);
            e.Property(c => c.Type).HasConversion<string>();
        });

        b.Entity<Product>(e => e.HasKey(p => p.Sku));

        b.Entity<CanonicalOrder>(e =>
        {
            // PO-number dedupe per channel (§8) as the natural key.
            e.HasKey(o => new { o.ChannelKey, o.OrderNumber });
            e.Property(o => o.Status).HasConversion<string>();
            e.OwnsOne(o => o.Customer, cb => cb.OwnsOne(c => c.DeliveryAddress));
            e.OwnsMany(o => o.Lines);
        });

        b.Entity<OrderResponse>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Status).HasConversion<string>();
        });

        b.Entity<PriceList>(e =>
        {
            e.HasKey(p => p.Id);
            e.OwnsMany(p => p.Items);
        });

        b.Entity<StockLevel>(e =>
        {
            e.HasKey(s => new { s.ChannelKey, s.Sku, s.Warehouse });
        });
    }
}
