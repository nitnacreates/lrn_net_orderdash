using Central.Core.Models;
using MockOrderWise;

var centralUrl = Environment.GetEnvironmentVariable("MOCK__CentralUrl") ?? "http://localhost:5148";
var pollSeconds = int.TryParse(Environment.GetEnvironmentVariable("MOCK__PollSeconds"), out var s) ? s : 30;

using var http = new HttpClient { BaseAddress = new Uri(centralUrl) };
http.DefaultRequestHeaders.Add("X-Api-Key", Environment.GetEnvironmentVariable("MOCK__ApiKey") ?? "dev-bridge-key");
var central = new CentralClient(http);

// MockOrderWise's own little ERP store (§7) — the real bridge imports into OrderWise instead.
var imported = new List<CanonicalOrder>();
var dispatched = new HashSet<string>();

Console.WriteLine($"MockOrderWise bridge -> {centralUrl} (poll every {pollSeconds}s). Ctrl+C to stop.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.IsCancellationRequested)
{
    try
    {
        // pull: import new orders from Central
        var pending = await central.PullPendingAsync(cts.Token);
        imported.AddRange(pending);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] pulled {pending.Count} new order(s); imported total {imported.Count}.");

        // push: "dispatch" the next order and send it back — proves the full round trip
        var next = imported.FirstOrDefault(o => !dispatched.Contains(o.OrderNumber));
        if (next is not null)
        {
            var dispatch = new CanonicalDispatch
            {
                ChannelKey = next.ChannelKey,
                OrderNumber = next.OrderNumber,
                DispatchedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Carrier = "DPD",
                TrackingNumber = $"TRK{next.OrderNumber}"
            };

            var result = await central.PushDispatchAsync(dispatch, $"mock-{next.OrderNumber}", cts.Token);
            dispatched.Add(next.OrderNumber);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] dispatched {next.OrderNumber} -> {result?.Status}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] cycle failed: {ex.Message}");
    }

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(pollSeconds), cts.Token);
    }
    catch (TaskCanceledException)
    {
        // shutting down
    }
}
