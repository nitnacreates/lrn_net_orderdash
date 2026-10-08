using Central.Core.Abstractions;
using Central.Core.Enums;
using Central.Core.Models;
using System.Globalization;
using System.Text.Json;

namespace Central.Channels;

/// <summary>
/// Template D: API + GraphQL (Debenhams, §9). The real transport is a GraphQL call returning
/// {"data":{"orders":[...]}}; for the MVP the response is a local JSON folder, same trick as the
/// other templates. The only difference from ApiRest is the envelope and the mutation on push.
/// </summary>
public class ApiGraphQlConnector(string channelKey, string inboundDirectory, string outboundDirectory)
    : IPartnerConnector
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ChannelType Type => ChannelType.ApiGraphQl;

    public IReadOnlySet<DocumentKind> Supported { get; } =
        new HashSet<DocumentKind> { DocumentKind.Orders, DocumentKind.OrderResponse };

    public Task<IReadOnlyList<CanonicalOrder>> PullOrdersAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(inboundDirectory);

        var orders = new List<CanonicalOrder>();
        foreach (var file in Directory.EnumerateFiles(inboundDirectory, "*.json"))
        {
            // GraphQL responses wrap the payload in a "data" object.
            var envelope = JsonSerializer.Deserialize<GraphQlResponse>(File.ReadAllText(file), Json);
            orders.AddRange((envelope?.Data?.Orders ?? []).Select(Map));

            File.Move(file, file + ".done", overwrite: true);
        }

        return Task.FromResult<IReadOnlyList<CanonicalOrder>>(orders);
    }

    public Task PushAsync(DocumentKind kind, object document, CancellationToken ct = default)
    {
        if (kind != DocumentKind.OrderResponse)
        {
            throw new NotSupportedException($"{Type} does not push {kind}.");
        }

        var response = (OrderResponse)document;
        Directory.CreateDirectory(outboundDirectory);

        var path = Path.Combine(outboundDirectory, $"{response.OrderNumber}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            data = new
            {
                createOrderResponse = new
                {
                    purchaseOrderNumber = response.OrderNumber,
                    status = response.Status.ToString(),
                    reason = response.Reason,
                    dispatchDate = response.DispatchedDate?.ToString("yyyy-MM-dd"),
                    carrier = response.Carrier,
                    trackingNumber = response.TrackingNumber
                }
            }
        }, Json));

        return Task.CompletedTask;
    }

    /// <summary>GraphQL order shape -> canonical (§10 mapping).</summary>
    private CanonicalOrder Map(Node o) => new()
    {
        ChannelKey = channelKey,
        OrderNumber = o.PurchaseOrderNumber,
        CustomerRef = o.CustomerOrderRef,
        OrderDate = ParseDate(o.OrderDate),
        RequiredDate = ParseDate(o.DeliveryDate),
        Customer = new CanonicalCustomer
        {
            Name = o.Customer?.Name ?? string.Empty,
            Phone = o.Customer?.Phone,
            Email = o.Customer?.Email,
            DeliveryAddress = new CanonicalAddress
            {
                Line1 = o.DeliveryAddress?.Line1,
                Line2 = o.DeliveryAddress?.Line2,
                Town = o.DeliveryAddress?.Town,
                County = o.DeliveryAddress?.County,
                Country = o.DeliveryAddress?.Country,
                Postcode = o.DeliveryAddress?.Postcode
            }
        },
        Lines = (o.Lines ?? []).Select(l => new CanonicalOrderLine
        {
            Sku = l.Sku,
            LineRef = l.LineRef,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList()
    };

    private static DateOnly ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var date)
            ? date
            : DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record GraphQlResponse(NodeData? Data);

    private sealed record NodeData(List<Node>? Orders);

    private sealed record Node(
        string PurchaseOrderNumber,
        string? CustomerOrderRef,
        string? OrderDate,
        string? DeliveryDate,
        NodeCustomer? Customer,
        NodeAddress? DeliveryAddress,
        List<NodeLine>? Lines);

    private sealed record NodeCustomer(string? Name, string? Phone, string? Email);

    private sealed record NodeAddress(
        string? Line1, string? Line2, string? Town, string? County, string? Country, string? Postcode);

    private sealed record NodeLine(string Sku, string? LineRef, int Quantity, decimal UnitPrice);
}
