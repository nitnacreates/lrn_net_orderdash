using Central.Core.Abstractions;
using Central.Core.Enums;
using Central.Core.Models;
using System.Globalization;
using System.Text.Json;

namespace Central.Channels;

/// <summary>
/// Template C: API + REST/JSON (Tesco, §9). The real transport is an HTTP call to the partner API;
/// for the MVP the "remote API" is a local JSON folder — the same trick FtpCsvConnector uses for FTP.
/// </summary>
public class ApiRestConnector(string channelKey, string inboundDirectory, string outboundDirectory)
    : IPartnerConnector
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ChannelType Type => ChannelType.ApiRest;

    public Task<IReadOnlyList<CanonicalOrder>> PullOrdersAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(inboundDirectory);

        var orders = new List<CanonicalOrder>();
        foreach (var file in Directory.EnumerateFiles(inboundDirectory, "*.json"))
        {
            var payload = JsonSerializer.Deserialize<List<TescoOrder>>(File.ReadAllText(file), Json) ?? [];
            orders.AddRange(payload.Select(Map));

            File.Move(file, file + ".done", overwrite: true);
        }

        return Task.FromResult<IReadOnlyList<CanonicalOrder>>(orders);
    }

    public Task PushDispatchAsync(CanonicalDispatch dispatch, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outboundDirectory);

        var path = Path.Combine(outboundDirectory, $"{dispatch.OrderNumber}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            purchaseOrderNumber = dispatch.OrderNumber,
            dispatchDate = dispatch.DispatchedDate.ToString("yyyy-MM-dd"),
            carrier = dispatch.Carrier,
            trackingNumber = dispatch.TrackingNumber
        }, Json));

        return Task.CompletedTask;
    }

    /// <summary>Tesco JSON -> canonical (§10 mapping, same shape as the CSV template).</summary>
    private CanonicalOrder Map(TescoOrder o) => new()
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

    // Tesco's API shape — nested so the template stays self-contained.
    private sealed record TescoOrder(
        string PurchaseOrderNumber,
        string? CustomerOrderRef,
        string? OrderDate,
        string? DeliveryDate,
        TescoCustomer? Customer,
        TescoAddress? DeliveryAddress,
        List<TescoLine>? Lines);

    private sealed record TescoCustomer(string? Name, string? Phone, string? Email);

    private sealed record TescoAddress(
        string? Line1, string? Line2, string? Town, string? County, string? Country, string? Postcode);

    private sealed record TescoLine(string Sku, string? LineRef, int Quantity, decimal UnitPrice);
}
