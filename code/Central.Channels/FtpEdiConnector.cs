using Central.Core.Abstractions;
using Central.Core.Enums;
using Central.Core.Models;
using System.Globalization;

namespace Central.Channels;

/// <summary>
/// Template B: FTP + EDI (Dunelm, §9). EDI is the heaviest of the four — here it is a simplified
/// segment format (one segment per line, fields pipe-delimited) over the same local-folder FTP stub.
/// Segments: HDR (order), CUS (customer), ADR (address), LIN (one per line).
/// </summary>
public class FtpEdiConnector(string channelKey, string inboundDirectory, string outboundDirectory)
    : IPartnerConnector
{
    public ChannelType Type => ChannelType.FtpEdi;

    public Task<IReadOnlyList<CanonicalOrder>> PullOrdersAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(inboundDirectory);

        var orders = new List<CanonicalOrder>();
        foreach (var file in Directory.EnumerateFiles(inboundDirectory, "*.edi"))
        {
            orders.AddRange(Parse(File.ReadAllLines(file)));
            File.Move(file, file + ".done", overwrite: true);
        }

        return Task.FromResult<IReadOnlyList<CanonicalOrder>>(orders);
    }

    public Task PushDispatchAsync(CanonicalDispatch dispatch, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outboundDirectory);

        var path = Path.Combine(outboundDirectory, $"{channelKey}-{dispatch.OrderNumber}.edi");
        File.WriteAllLines(path,
        [
            "DSP|PurchaseOrderNumber|DespatchDate|Carrier|TrackingNumber",
            $"DSP|{dispatch.OrderNumber}|{dispatch.DispatchedDate:yyyy-MM-dd}|{dispatch.Carrier}|{dispatch.TrackingNumber}"
        ]);

        return Task.CompletedTask;
    }

    /// <summary>EDI segments -> canonical (§10 mapping).</summary>
    private IEnumerable<CanonicalOrder> Parse(IReadOnlyList<string> lines)
    {
        CanonicalOrder? order = null;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var f = line.Split('|');
            string Get(int i) => i < f.Length ? f[i].Trim() : string.Empty;

            switch (Get(0))
            {
                case "HDR":
                    order = new CanonicalOrder
                    {
                        ChannelKey = channelKey,
                        OrderNumber = Get(1),
                        CustomerRef = Get(2),
                        OrderDate = ParseDate(Get(3)),
                        RequiredDate = ParseDate(Get(4))
                    };
                    break;

                case "CUS" when order is not null:
                    order.Customer.Name = Get(1);
                    order.Customer.Phone = Get(2);
                    order.Customer.Email = Get(3);
                    break;

                case "ADR" when order is not null:
                    order.Customer.DeliveryAddress = new CanonicalAddress
                    {
                        Line1 = Get(1),
                        Line2 = Get(2),
                        Town = Get(3),
                        County = Get(4),
                        Country = Get(5),
                        Postcode = Get(6)
                    };
                    break;

                case "LIN" when order is not null:
                    order.Lines.Add(new CanonicalOrderLine
                    {
                        LineRef = Get(1),
                        Sku = Get(2),
                        Quantity = int.TryParse(Get(3), out var quantity) ? quantity : 0,
                        UnitPrice = decimal.TryParse(Get(4), CultureInfo.InvariantCulture, out var price) ? price : 0
                    });
                    break;

                case "END":
                    if (order is not null)
                    {
                        yield return order;
                        order = null;
                    }
                    break;
            }
        }

        // tolerate files that omit an explicit END segment
        if (order is not null && !string.IsNullOrWhiteSpace(order.OrderNumber))
        {
            yield return order;
        }
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var date)
            ? date
            : DateOnly.FromDateTime(DateTime.UtcNow);
}
