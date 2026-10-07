using Central.Core.Abstractions;
using Central.Core.Enums;
using Central.Core.Models;
using System.Globalization;

namespace Central.Channels;

/// <summary>
/// Template A: FTP + CSV (ASDA, §9). Real FTP is stubbed — it reads/writes a local folder,
/// which is exactly how an FTP server looks to a connector. Mapping follows §10.2.
/// </summary>
public class FtpCsvConnector(string inboundDirectory, string outboundDirectory) : IPartnerConnector
{
    public ChannelType Type => ChannelType.FtpCsv;

    public Task<IReadOnlyList<CanonicalOrder>> PullOrdersAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(inboundDirectory);

        var orders = new List<CanonicalOrder>();
        foreach (var file in Directory.EnumerateFiles(inboundDirectory, "*.csv"))
        {
            orders.AddRange(Parse(File.ReadAllLines(file)));

            // mark as handled on the "FTP box" rather than deleting it
            File.Move(file, file + ".done", overwrite: true);
        }

        return Task.FromResult<IReadOnlyList<CanonicalOrder>>(orders);
    }

    public Task PushDispatchAsync(CanonicalDispatch dispatch, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outboundDirectory);

        var path = Path.Combine(outboundDirectory, $"{dispatch.ChannelKey}-{dispatch.OrderNumber}.csv");
        File.WriteAllLines(path,
        [
            "PurchaseOrderNumber,DespatchDate,Carrier,TrackingNumber",
            $"{dispatch.OrderNumber},{dispatch.DispatchedDate:yyyy-MM-dd},{dispatch.Carrier},{dispatch.TrackingNumber}"
        ]);

        return Task.CompletedTask;
    }

    /// <summary>ASDA CSV columns -> canonical (§10.2).</summary>
    private static IEnumerable<CanonicalOrder> Parse(IEnumerable<string> lines)
    {
        var rows = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (rows.Count < 2)
        {
            yield break;
        }

        var index = rows[0].Split(',')
            .Select((name, i) => (name: name.Trim(), i))
            .ToDictionary(x => x.name, x => x.i, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Skip(1))
        {
            var cells = row.Split(',');
            string Get(string column) =>
                index.TryGetValue(column, out var i) && i < cells.Length ? cells[i].Trim() : string.Empty;

            var orderNumber = Get("PurchaseOrderNumber");
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                continue;
            }

            yield return new CanonicalOrder
            {
                ChannelKey = "asda",
                OrderNumber = orderNumber,
                CustomerRef = Get("SalesOrderNumber"),
                OrderDate = ParseDate(Get("OrderDate")),
                RequiredDate = ParseDate(Get("EstimatedDeliveryDate")),
                Customer = new CanonicalCustomer
                {
                    Name = $"{Get("CustomerFirstName")} {Get("CustomerLastName")}".Trim(),
                    Phone = Get("Phone"),
                    Email = Get("Email"),
                    DeliveryAddress = new CanonicalAddress
                    {
                        Line1 = Get("DeliveryAddressLine1"),
                        Line2 = Get("DeliveryAddressLine2"),
                        Town = Get("DeliveryAddressTown"),
                        County = Get("DeliveryAddressCounty"),
                        Country = Get("DeliveryCountry"),
                        Postcode = Get("DeliveryPostCode")
                    }
                },
                Lines =
                [
                    new CanonicalOrderLine
                    {
                        Sku = Get("ItemId"),
                        LineRef = Get("PurchaseOrderLineNumber"),
                        Quantity = int.TryParse(Get("Quantity"), out var quantity) ? quantity : 0
                    }
                ]
            };
        }
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var date)
            ? date
            : DateOnly.FromDateTime(DateTime.UtcNow);
}
