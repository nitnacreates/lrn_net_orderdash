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
        CanonicalDispatch dispatch, string idempotencyKey, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/dispatch")
        {
            Content = JsonContent.Create(dispatch, options: Json)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await http.SendAsync(request, ct);
        return await response.Content.ReadFromJsonAsync<DispatchResult>(Json, ct);
    }
}
