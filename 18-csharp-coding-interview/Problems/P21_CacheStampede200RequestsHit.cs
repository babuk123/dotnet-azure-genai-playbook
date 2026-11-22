namespace Interview;

// #21 Cache stampede: 200 requests hit one expired key  [Advanced]
// Problem : A hot cache entry expires and 200 concurrent requests all call the database. Make only one request load the data while the others wait.
// Approach: Per-key lock (SemaphoreSlim) with double-check, or share one Lazy<Task<T>> per key. In .NET 9+ HybridCache provides this protection built in.
// Complexity: One database call per expiry instead of one per request.

public class StampedeSafeCache<TValue>
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<Task<TValue>>> _inflight = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (TValue Value, DateTime Expires)> _cache = new();

    public async Task<TValue> GetOrLoadAsync(string key, Func<Task<TValue>> loader, TimeSpan ttl)
    {
        if (_cache.TryGetValue(key, out var hit) && hit.Expires > DateTime.UtcNow)
            return hit.Value;

        // All concurrent callers share ONE load task for this key
        var lazy = _inflight.GetOrAdd(key, k => new Lazy<Task<TValue>>(async () =>
        {
            try
            {
                var value = await loader();
                _cache[key] = (value, DateTime.UtcNow.Add(ttl));
                return value;
            }
            finally
            {
                _inflight.TryRemove(key, out _);
            }
        }));
        return await lazy.Value;
    }
}
