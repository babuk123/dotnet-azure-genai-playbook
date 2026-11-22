namespace Interview;

// #14 Call many APIs in parallel with a concurrency limit  [Intermediate]
// Problem : Given a list of IDs and an async GetCustomerAsync(id), fetch all of them in parallel but never more than 5 at a time.
// Approach: Option 1: Parallel.ForEachAsync with MaxDegreeOfParallelism (.NET 6+). Option 2: SemaphoreSlim + Task.WhenAll.
// Complexity: Total time ≈ (n / 5) × call time.

public static class Throttled
{
    // Option 1: .NET 6+ built-in
    public static async Task<List<string>> FetchAllAsync(
        IEnumerable<int> ids, Func<int, CancellationToken, Task<string>> getCustomer, CancellationToken ct = default)
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();
        await Parallel.ForEachAsync(ids,
            new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = ct },
            async (id, token) => results.Add(await getCustomer(id, token)));
        return results.ToList();
    }

    // Option 2: SemaphoreSlim (keeps original order)
    public static async Task<string[]> FetchAllOrderedAsync(
        IEnumerable<int> ids, Func<int, Task<string>> getCustomer, int maxConcurrency = 5)
    {
        using var gate = new SemaphoreSlim(maxConcurrency);
        var tasks = ids.Select(async id =>
        {
            await gate.WaitAsync();
            try { return await getCustomer(id); }
            finally { gate.Release(); }
        });
        return await Task.WhenAll(tasks);
    }
}
