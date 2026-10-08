using Central.Core.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MockOrderWise;

/// <summary>
/// Talks only to Central, over the same two endpoints the real OrderWise bridge will use (§7).
/// </summary>
public class CentralClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<List<CanonicalOrder>> PullPendingAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<CanonicalOrder>>("/api/orders/pending", Json, ct) ?? [];

    public async Task<DispatchResult?> PushDispatchAsync(
        OrderResponse response, string idempotencyKey, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/dispatch")
        {
            Content = JsonContent.Create(response, options: Json)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        using var httpResponse = await http.SendAsync(request, ct);
        return await httpResponse.Content.ReadFromJsonAsync<DispatchResult>(Json, ct);
    }

    /// <summary>ERP is the master for price (§14): push a channel's price list to Central.</summary>
    public async Task PushPriceAsync(PriceList price, CancellationToken ct = default)
    {
        using var httpResponse = await http.PostAsJsonAsync("/api/price", price, Json, ct);
        httpResponse.EnsureSuccessStatusCode();
    }

    /// <summary>ERP is the master for stock (§14): push stock levels to Central.</summary>
    public async Task PushStockAsync(List<StockLevel> levels, CancellationToken ct = default)
    {
        using var httpResponse = await http.PostAsJsonAsync("/api/stock", levels, Json, ct);
        httpResponse.EnsureSuccessStatusCode();
    }
}
