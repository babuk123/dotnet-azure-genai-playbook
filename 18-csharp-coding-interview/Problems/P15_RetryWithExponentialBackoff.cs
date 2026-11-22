namespace Interview;

// #15 Retry with exponential backoff  [Intermediate]
// Problem : Write a helper that retries an async operation up to N times on transient errors, waiting longer each time.
// Approach: Loop with try/catch; delay = baseDelay × 2^attempt (+ jitter); rethrow after the last attempt. In production use Polly / Microsoft.Extensions.Http.Resilience.
// Complexity: Worst case waits ≈ base × (2^(n−1) − 1) plus jitter.

public static class Retry
{
    public static async Task<T> WithBackoffAsync<T>(
        Func<Task<T>> operation, int maxAttempts = 3, int baseDelayMs = 200,
        Func<Exception, bool>? isTransient = null)
    {
        isTransient ??= ex => ex is HttpRequestException or TimeoutException;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxAttempts && isTransient(ex))
            {
                int delay = baseDelayMs * (1 << (attempt - 1)) + Random.Shared.Next(0, 100); // jitter
                await Task.Delay(delay);
            }
        }
    }
}
