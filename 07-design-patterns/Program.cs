// Design patterns used in real .NET services, wired with the built-in DI container.
using Microsoft.Extensions.DependencyInjection;

System.Globalization.CultureInfo.CurrentCulture = new("en-US");

var services = new ServiceCollection();
services.AddSingleton<ICustomerRepository, SqlCustomerRepository>();
services.Decorate<ICustomerRepository, CachedCustomerRepository>();          // Decorator
services.AddKeyedSingleton<IFeeStrategy, StandardFee>("standard");           // Strategy (.NET 8 keyed services)
services.AddKeyedSingleton<IFeeStrategy, PremiumFee>("premium");
services.AddSingleton<IMediator, SimpleMediator>();                          // Mediator
services.AddSingleton<IRequestHandler<GetCustomerQuery, string>, GetCustomerHandler>();
var sp = services.BuildServiceProvider();

Console.WriteLine("=== Strategy: pick fee rule at run time ===");
foreach (var tier in new[] { "standard", "premium" })
{
    var fee = sp.GetRequiredKeyedService<IFeeStrategy>(tier);
    Console.WriteLine($"{tier,-8} fee on $10,000 = {fee.Calculate(10_000m):C}");
}

Console.WriteLine("\n=== Decorator: add caching without touching the repository ===");
var repo = sp.GetRequiredService<ICustomerRepository>();
Console.WriteLine(repo.Get(7)); Console.WriteLine(repo.Get(7));   // second call served from cache

Console.WriteLine("\n=== Mediator: controller sends a request, handler is found for it ===");
var mediator = sp.GetRequiredService<IMediator>();
Console.WriteLine(mediator.Send<GetCustomerQuery, string>(new GetCustomerQuery(42)));

Console.WriteLine("\n=== Outbox: save state + event in one transaction, publish later ===");
var db = new FakeDb();
db.SaveOrderWithOutbox(orderId: 1001);
new OutboxPublisher(db).PublishPending();
new OutboxPublisher(db).PublishPending();   // nothing left to publish -> no duplicates

Console.WriteLine("\n=== Retry with exponential backoff ===");
int attempts = 0;
var result = await RetryPolicy.ExecuteAsync(async () =>
{
    attempts++;
    await Task.Delay(10);
    if (attempts < 3) throw new TimeoutException($"attempt {attempts} timed out");
    return "payment gateway OK";
});
Console.WriteLine($"{result} after {attempts} attempts");

// ---------------- Strategy ----------------
public interface IFeeStrategy { decimal Calculate(decimal amount); }
public class StandardFee : IFeeStrategy { public decimal Calculate(decimal a) => Math.Max(5m, a * 0.01m); }
public class PremiumFee : IFeeStrategy { public decimal Calculate(decimal a) => 0m; }

// ---------------- Decorator ----------------
public interface ICustomerRepository { string Get(int id); }
public class SqlCustomerRepository : ICustomerRepository
{
    public string Get(int id) { Console.WriteLine($"  (SQL query for customer {id})"); return $"Customer {id}"; }
}
public class CachedCustomerRepository(ICustomerRepository inner) : ICustomerRepository
{
    private readonly Dictionary<int, string> _cache = new();
    public string Get(int id) => _cache.TryGetValue(id, out var c) ? $"{c} (cache)" : _cache[id] = inner.Get(id);
}

// ---------------- Mediator ----------------
public record GetCustomerQuery(int Id);
public interface IRequestHandler<TRequest, TResponse> { TResponse Handle(TRequest request); }
public interface IMediator { TResponse Send<TRequest, TResponse>(TRequest request); }
public class SimpleMediator(IServiceProvider sp) : IMediator
{
    public TResponse Send<TRequest, TResponse>(TRequest request) =>
        sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>().Handle(request);
}
public class GetCustomerHandler(ICustomerRepository repo) : IRequestHandler<GetCustomerQuery, string>
{
    public string Handle(GetCustomerQuery q) => $"Handler -> {repo.Get(q.Id)}";
}

// ---------------- Outbox ----------------
public class FakeDb
{
    public List<int> Orders { get; } = new();
    public List<(Guid Id, string Event, bool Sent)> Outbox { get; } = new();
    public void SaveOrderWithOutbox(int orderId)
    {
        // In SQL: BEGIN TRAN; INSERT Orders; INSERT Outbox; COMMIT;  -> both or neither
        Orders.Add(orderId);
        Outbox.Add((Guid.NewGuid(), $"OrderCreated:{orderId}", false));
        Console.WriteLine($"saved order {orderId} + OrderCreated event in one transaction");
    }
}
public class OutboxPublisher(FakeDb db)
{
    public void PublishPending()
    {
        var pending = db.Outbox.Where(e => !e.Sent).ToList();
        Console.WriteLine($"publisher found {pending.Count} pending event(s)");
        foreach (var e in pending)
        {
            Console.WriteLine($"  -> published {e.Event} to Service Bus");
            db.Outbox[db.Outbox.IndexOf(e)] = e with { Sent = true };
        }
    }
}

// ---------------- Retry ----------------
public static class RetryPolicy
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> op, int max = 4, int baseMs = 50)
    {
        for (int attempt = 1; ; attempt++)
        {
            try { return await op(); }
            catch (TimeoutException ex) when (attempt < max)
            {
                var delay = baseMs * (1 << (attempt - 1));
                Console.WriteLine($"  {ex.Message}; retrying in {delay} ms");
                await Task.Delay(delay);
            }
        }
    }
}

// ---------------- tiny Decorate helper (Scrutor offers this as a package) ----------------
public static class DecorateExtensions
{
    public static IServiceCollection Decorate<TService, TDecorator>(this IServiceCollection services)
        where TService : class where TDecorator : class, TService
    {
        var original = services.Last(d => d.ServiceType == typeof(TService));
        services.Remove(original);
        services.Add(new ServiceDescriptor(typeof(TService), sp =>
        {
            var inner = (TService)(original.ImplementationInstance
                ?? original.ImplementationFactory?.Invoke(sp)
                ?? ActivatorUtilities.CreateInstance(sp, original.ImplementationType!));
            return ActivatorUtilities.CreateInstance<TDecorator>(sp, inner);
        }, original.Lifetime));
        return services;
    }
}
