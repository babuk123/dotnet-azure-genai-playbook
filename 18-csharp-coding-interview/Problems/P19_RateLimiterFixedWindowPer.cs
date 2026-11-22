namespace Interview;

// #19 Rate limiter (fixed window per client)  [Advanced]
// Problem : Allow each client at most N requests per window (e.g., 100 per minute). Make it thread-safe.
// Approach: Keep a counter and window start per client in a ConcurrentDictionary; lock per client entry. Mention ASP.NET Core's built-in AddRateLimiter (fixed window, sliding window, token bucket, concurrency) and APIM rate-limit-by-key for production.
// Complexity: O(1) per request.

public class FixedWindowRateLimiter(int limit, TimeSpan window)
{
    private sealed class Counter { public DateTime WindowStart; public int Count; }
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Counter> _clients = new();

    public bool TryAcquire(string clientId)
    {
        var c = _clients.GetOrAdd(clientId, _ => new Counter { WindowStart = DateTime.UtcNow });
        lock (c)
        {
            var now = DateTime.UtcNow;
            if (now - c.WindowStart >= window)
            {
                c.WindowStart = now;   // new window
                c.Count = 0;
            }
            if (c.Count >= limit) return false;   // -> HTTP 429
            c.Count++;
            return true;
        }
    }
}

// Built-in alternative in ASP.NET Core:
// builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("api", opt =>
//     { opt.PermitLimit = 100; opt.Window = TimeSpan.FromMinutes(1); }));
// app.UseRateLimiter();  app.MapGet("/orders", ...).RequireRateLimiting("api");
