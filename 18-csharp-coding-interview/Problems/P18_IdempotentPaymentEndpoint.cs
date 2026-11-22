namespace Interview;

// #18 Idempotent payment endpoint  [Advanced]
// Problem : Clients may retry POST /payments after a timeout. Make sure a payment is processed only once.
// Approach: Client sends an Idempotency-Key header. Store key → result; if the key exists, return the stored result. Use an atomic insert (unique constraint / TryAdd) so two concurrent retries cannot both process.
// Complexity: O(1) per request.

public record PaymentRequest(string FromAccount, string ToAccount, decimal Amount);
public record PaymentResult(Guid PaymentId, string Status);

public class IdempotentPaymentService
{
    // In production: a DB table with a UNIQUE index on IdempotencyKey (or Redis SET NX)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<Task<PaymentResult>>> _store = new();
    public int ProcessedCount;

    public Task<PaymentResult> PayAsync(string idempotencyKey, PaymentRequest req)
    {
        // GetOrAdd + Lazy guarantees the payment logic runs once per key, even for concurrent retries
        var lazy = _store.GetOrAdd(idempotencyKey,
            _ => new Lazy<Task<PaymentResult>>(() => ProcessAsync(req)));
        return lazy.Value;
    }

    private async Task<PaymentResult> ProcessAsync(PaymentRequest req)
    {
        Interlocked.Increment(ref ProcessedCount);
        await Task.Delay(50);                       // call core banking / payment gateway
        return new PaymentResult(Guid.NewGuid(), "Completed");
    }
}

// Minimal API endpoint
// app.MapPost("/payments", async ([FromHeader(Name = "Idempotency-Key")] string key,
//                                PaymentRequest req, IdempotentPaymentService svc) =>
//     Results.Ok(await svc.PayAsync(key, req)));
