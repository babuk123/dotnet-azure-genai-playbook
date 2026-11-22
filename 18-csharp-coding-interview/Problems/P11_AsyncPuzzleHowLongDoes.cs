namespace Interview;

// #11 Async puzzle: how long does this code take?  [Intermediate]
// Problem : Each call waits 1 second. How long does Sequential take? Parallel? And what is the difference between await DoCheckAsync() and await Task.Run(async () => await DoCheckAsync())?
// Approach: Sequential awaits add up (~3 s). Starting all tasks first and then awaiting Task.WhenAll runs them concurrently (~1 s). Task.Run just moves the start of the work to a thread-pool thread — for I/O-bound async code it adds overhead and does not make it faster; use it only to offload CPU-bound work.
// Complexity: Sequential ≈ sum of waits; WhenAll ≈ the longest wait.

public static class AsyncPuzzle
{
    public static async Task<bool> DoCheckAsync()
    {
        await Task.Delay(1000); // simulates an I/O call (DB / HTTP)
        return true;
    }

    public static async Task Sequential()      // ~3 seconds
    {
        await DoCheckAsync();
        await DoCheckAsync();
        await DoCheckAsync();
    }

    public static async Task Parallel()        // ~1 second
    {
        Task<bool> t1 = DoCheckAsync();   // all three start immediately
        Task<bool> t2 = DoCheckAsync();
        Task<bool> t3 = DoCheckAsync();
        await Task.WhenAll(t1, t2, t3);
    }

    // Same ~1 s, but wastes a thread-pool hop for I/O-bound work
    public static Task<bool> WrappedInTaskRun() => Task.Run(async () => await DoCheckAsync());
}
