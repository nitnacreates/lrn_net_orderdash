using Central.Core.Abstractions;
using Central.Core.Enums;
using Central.Core.Models;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace Central.Channels;

/// <summary>
/// Template E: FTP + XML with XSLT maps (§9, §15, §16). The one transport built for real.
/// Three-folder contract: consume orders/ in; drop price/, stock/ and responses/ out.
/// Every mapping goes through an .xsl map — the partner's schema is their contract, not ours.
/// </summary>
public class FtpXmlConnector(
    string channelKey,
    string ordersDirectory,
    string priceDirectory,
    string stockDirectory,
    string responsesDirectory,
    string xsltRoot) : IPartnerConnector
{
    public ChannelType Type => ChannelType.FtpXml;

    public IReadOnlySet<DocumentKind> Supported { get; } = new HashSet<DocumentKind>
    {
        DocumentKind.Orders, DocumentKind.OrderResponse, DocumentKind.Price, DocumentKind.Stock
    };

    /// <summary>orders/ in — each file goes through order_inbound.xsl, then to processed/.</summary>
    public Task<IReadOnlyList<CanonicalOrder>> PullOrdersAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(ordersDirectory);

        var orders = new List<CanonicalOrder>();
        foreach (var file in Directory.EnumerateFiles(ordersDirectory, "*.xml"))
        {
            var canonical = Transform("order_inbound.xsl", XDocument.Load(file), ("channelKey", channelKey));
            var order = ReadOrder(canonical);
            if (order is not null)
            {
                orders.Add(order);
            }

            Move(file, "processed");
        }

        return Task.FromResult<IReadOnlyList<CanonicalOrder>>(orders);
    }

    /// <summary>price/, stock/ and responses/ out — build canonical XML, run the map, drop the file.</summary>
    public Task PushAsync(DocumentKind kind, object document, CancellationToken ct = default)
    {
        var (map, xml, directory, prefix) = kind switch
        {
            DocumentKind.OrderResponse => ("orderresponse_outbound.xsl", BuildResponseXml((OrderResponse)document), responsesDirectory, "response"),
            DocumentKind.Price => ("price_outbound.xsl", BuildPriceXml((PriceList)document), priceDirectory, "price"),
            DocumentKind.Stock => ("stock_outbound.xsl", BuildStockXml((IReadOnlyList<StockLevel>)document), stockDirectory, "stock"),
            _ => throw new NotSupportedException($"{Type} does not push {kind}.")
        };

        Directory.CreateDirectory(directory);
        var transformed = Transform(map, xml);
        transformed.Save(Path.Combine(directory, $"{prefix}_{channelKey}_{DateTime.UtcNow:yyyyMMddHHmmss}.xml"));

        return Task.CompletedTask;
    }

    /// <summary>Runs an XSLT map, preferring a channel-specific map and falling back to the generic one (§16).</summary>
    private XDocument Transform(string mapName, XDocument input, params (string Name, string Value)[] parameters)
    {
        var channelMap = Path.Combine(xsltRoot, channelKey, mapName);
        var map = File.Exists(channelMap) ? channelMap : Path.Combine(xsltRoot, "generic", mapName);

        var xslt = new XslCompiledTransform();
        xslt.Load(map);

        var args = new XsltArgumentList();
        foreach (var (name, value) in parameters)
        {
            args.AddParam(name, string.Empty, value);
        }

        var writer = new StringWriter();
        using (var reader = input.CreateReader())
        {
            xslt.Transform(reader, args, writer);
        }

        return XDocument.Parse(writer.ToString());
    }

    /// <summary>Canonical XML from order_inbound.xsl -> CanonicalOrder.</summary>
    private CanonicalOrder? ReadOrder(XDocument doc)
    {
        var root = doc.Root;
        var orderNumber = (string?)root?.Element("orderNumber");
        if (root is null || string.IsNullOrWhiteSpace(orderNumber))
        {
            return null;
        }

        string? Get(string name) => (string?)root.Element(name);

        return new CanonicalOrder
        {
            ChannelKey = Get("channelKey") ?? channelKey,
            OrderNumber = orderNumber,
            CustomerRef = Get("customerRef"),
            OrderDate = ParseDate(Get("orderDate")),
            RequiredDate = ParseDate(Get("requiredDate")),
            Customer = new CanonicalCustomer
            {
                Name = Get("customerName") ?? string.Empty,
                Phone = Get("phone"),
                Email = Get("email"),
                DeliveryAddress = new CanonicalAddress
                {
                    Line1 = Get("line1"),
                    Line2 = Get("line2"),
                    Town = Get("town"),
                    County = Get("county"),
                    Country = Get("country"),
                    Postcode = Get("postcode")
                }
            },
            Lines = root.Element("lines")?.Elements("line").Select(line => new CanonicalOrderLine
            {
                Sku = (string?)line.Element("sku") ?? string.Empty,
                LineRef = (string?)line.Element("lineRef"),
                Quantity = int.TryParse((string?)line.Element("quantity"), out var quantity) ? quantity : 0,
                UnitPrice = decimal.TryParse((string?)line.Element("unitPrice"),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice) ? unitPrice : 0,
                TaxCode = (string?)line.Element("taxCode")
            }).ToList() ?? []
        };
    }

    private static XDocument BuildResponseXml(OrderResponse r) => new(
        new XElement("response",
            new XElement("channelKey", r.ChannelKey),
            new XElement("orderNumber", r.OrderNumber),
            new XElement("status", r.Status.ToString()),
            new XElement("reason", r.Reason ?? string.Empty),
            new XElement("dispatchedDate", r.DispatchedDate?.ToString("yyyy-MM-dd") ?? string.Empty),
            new XElement("carrier", r.Carrier ?? string.Empty),
            new XElement("trackingNumber", r.TrackingNumber ?? string.Empty)));

    private static XDocument BuildPriceXml(PriceList p) => new(
        new XElement("priceList",
            new XElement("effectiveFrom", p.EffectiveFrom.ToString("yyyy-MM-dd")),
            new XElement("effectiveTo", p.EffectiveTo?.ToString("yyyy-MM-dd") ?? string.Empty),
            new XElement("items", p.Items.Select(i => new XElement("item",
                new XAttribute("sku", i.Sku),
                new XAttribute("currency", i.Currency),
                new XAttribute("unitPrice", i.UnitPrice.ToString(CultureInfo.InvariantCulture)))))));

    private static XDocument BuildStockXml(IReadOnlyList<StockLevel> levels) => new(
        new XElement("stockLevels", levels.Select(l => new XElement("level",
            new XAttribute("sku", l.Sku),
            new XAttribute("warehouse", l.Warehouse),
            new XAttribute("onHand", l.QtyOnHand),
            new XAttribute("available", l.QtyAvailable),
            new XAttribute("asOf", l.AsOf.ToString("yyyy-MM-ddTHH:mm:ss"))))));

    private static void Move(string file, string subfolder)
    {
        var target = Path.Combine(Path.GetDirectoryName(file)!, subfolder);
        Directory.CreateDirectory(target);
        File.Move(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
    }

    private static DateOnly ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var date)
            ? date
            : DateOnly.FromDateTime(DateTime.UtcNow);
}
