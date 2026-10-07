using Central.Core.Enums;
using Central.Core.Models;

namespace Central.Api.Data;

/// <summary>Dummy data so the dashboard and the bridge loop have something to show (§13).</summary>
public static class DbSeeder
{
    public static void Seed(CentralDbContext db)
    {
        if (db.Products.Any())
        {
            return;
        }

        var products = BuildProducts().ToList();

        db.Products.AddRange(products);
        db.Channels.AddRange(BuildChannels());
        db.Orders.AddRange(BuildOrders(products));
        db.SyncLogs.AddRange(BuildLogs());
        db.SaveChanges();
    }

    private static IEnumerable<Product> BuildProducts()
    {
        var sizes = new[] { "Single", "Double", "King", "Super King" };

        // Pillows: firmness x pack
        foreach (var firmness in new[] { "Soft", "Medium", "Firm" })
        foreach (var pack in new[] { 1, 2, 4 })
            yield return new Product
            {
                Sku = $"PIL-{firmness[..3].ToUpper()}-{pack}",
                Name = $"{firmness} Pillow (pack of {pack})",
                Category = "Pillows",
                Variant = $"{firmness} / {pack}pk",
                Price = 9.99m * pack
            };

        // Duvets: tog x size
        foreach (var tog in new[] { 4.5m, 10.5m, 13.5m, 15m })
        foreach (var size in sizes)
            yield return new Product
            {
                Sku = $"DUV-{tog.ToString().Replace(".", "")}-{size.Replace(" ", "")}",
                Name = $"{tog} Tog Duvet - {size}",
                Category = "Duvets",
                Variant = $"{tog} tog / {size}",
                Price = 19.99m + tog
            };

        // Mattress Protectors: type x size
        foreach (var type in new[] { "Waterproof", "Quilted" })
        foreach (var size in sizes)
            yield return new Product
            {
                Sku = $"PRO-{type[..4].ToUpper()}-{size.Replace(" ", "")}",
                Name = $"{type} Mattress Protector - {size}",
                Category = "Mattress Protectors",
                Variant = $"{type} / {size}",
                Price = 14.99m
            };

        // Mattress Toppers: thickness x size
        foreach (var thickness in new[] { 2, 4 })
        foreach (var size in sizes)
            yield return new Product
            {
                Sku = $"TOP-{thickness}IN-{size.Replace(" ", "")}",
                Name = $"{thickness}\" Mattress Topper - {size}",
                Category = "Mattress Toppers",
                Variant = $"{thickness}\" / {size}",
                Price = 24.99m + thickness
            };

        // Pet Beds: type x size
        foreach (var type in new[] { "Donut", "Mat", "Crate" })
        foreach (var size in new[] { "S", "M", "L" })
            yield return new Product
            {
                Sku = $"PET-{type[..3].ToUpper()}-{size}",
                Name = $"{type} Pet Bed - {size}",
                Category = "Pet Beds",
                Variant = $"{type} / {size}",
                Price = 17.99m
            };
    }

    private static IEnumerable<Channel> BuildChannels() =>
    [
        // one of each of the four channel types (§9)
        new() { Key = "asda", Name = "ASDA", Type = ChannelType.FtpCsv, ScheduleMinutes = 15, Status = "green", LastSyncAt = DateTimeOffset.UtcNow.AddMinutes(-6) },
        new() { Key = "dunelm", Name = "Dunelm", Type = ChannelType.FtpEdi, ScheduleMinutes = 30, Status = "amber", LastSyncAt = DateTimeOffset.UtcNow.AddMinutes(-41) },
        new() { Key = "tesco", Name = "Tesco", Type = ChannelType.ApiRest, ScheduleMinutes = 30, Status = "green", LastSyncAt = DateTimeOffset.UtcNow.AddMinutes(-12) },
        new() { Key = "shopify", Name = "Shopify", Type = ChannelType.ApiRest, ScheduleMinutes = 60, Status = "green", LastSyncAt = DateTimeOffset.UtcNow.AddMinutes(-20) },
        new() { Key = "temu", Name = "Temu", Type = ChannelType.ApiRest, ScheduleMinutes = 30, Status = "red", LastSyncAt = DateTimeOffset.UtcNow.AddHours(-3) },
        new() { Key = "debenhams", Name = "Debenhams", Type = ChannelType.ApiGraphQl, ScheduleMinutes = 30, Status = "amber", LastSyncAt = DateTimeOffset.UtcNow.AddMinutes(-55) }
    ];

    private static IEnumerable<CanonicalOrder> BuildOrders(List<Product> products)
    {
        var rnd = new Random(42);
        var statuses = Enum.GetValues<OrderStatus>();
        var channels = new[] { "asda", "tesco", "temu", "shopify", "dunelm", "debenhams" };
        var towns = new[] { "Leeds", "Manchester", "Bristol", "Glasgow", "Cardiff" };

        var n = 0;
        foreach (var channel in channels)
        {
            for (var i = 0; i < 4; i++)
            {
                n++;
                var orderDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-rnd.Next(1, 30)));
                var lines = new List<CanonicalOrderLine>();
                for (var l = 0; l < rnd.Next(1, 4); l++)
                {
                    var product = products[rnd.Next(products.Count)];
                    lines.Add(new CanonicalOrderLine
                    {
                        Sku = product.Sku,
                        LineRef = (l + 1).ToString(),
                        Quantity = rnd.Next(1, 5),
                        UnitPrice = product.Price,
                        TaxCode = "T20"
                    });
                }

                yield return new CanonicalOrder
                {
                    ChannelKey = channel,
                    OrderNumber = $"{channel[..3].ToUpper()}-{orderDate:yyyyMMdd}-{n:0000}",
                    CustomerRef = $"WEB-{n:00000}",
                    OrderDate = orderDate,
                    RequiredDate = orderDate.AddDays(3),
                    Status = statuses[n % statuses.Length],
                    ReceivedAt = DateTimeOffset.UtcNow.AddDays(-rnd.Next(1, 30)),
                    Customer = new CanonicalCustomer
                    {
                        Name = $"Customer {n}",
                        Phone = "0113 000 0000",
                        Email = $"customer{n}@example.com",
                        DeliveryAddress = new CanonicalAddress
                        {
                            Line1 = $"{n} High Street",
                            Town = towns[n % towns.Length],
                            Country = "United Kingdom",
                            Postcode = $"LS{n % 10} 1AA"
                        }
                    },
                    Lines = lines
                };
            }
        }
    }

    private static IEnumerable<SyncLog> BuildLogs() =>
    [
        new() { ChannelKey = "asda", Direction = "pull", Level = "info", Message = "Downloaded 12 orders from FTP." },
        new() { ChannelKey = "tesco", Direction = "pull", Level = "info", Message = "Polled API - 3 new orders." },
        new() { ChannelKey = "temu", Direction = "push", Level = "error", Message = "Dispatch push failed: 401 Unauthorized." },
        new() { ChannelKey = "shopify", Direction = "push", Level = "info", Message = "Dispatch acknowledged." },
        new() { ChannelKey = "debenhams", Direction = "pull", Level = "warn", Message = "GraphQL returned a partial response." }
    ];
}
