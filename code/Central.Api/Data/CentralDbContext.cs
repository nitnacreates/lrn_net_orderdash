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
    }
}
