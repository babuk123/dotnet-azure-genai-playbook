// DDD aggregate + value object, domain events, CQRS read model, event-driven handlers,
// saga orchestration with compensation, and a circuit breaker. All in-memory and runnable.
using System.Globalization;
using Patterns;

CultureInfo.CurrentCulture = new("en-US");
var bus = new InMemoryEventBus();
var readModel = new LoanReadModel();
bus.Subscribe<LoanApproved>(readModel.On);                                    // projection (CQRS read side)
bus.Subscribe<LoanApproved>(e => Console.WriteLine($"  [notification] email customer about {e.LoanId}"));

Console.WriteLine("=== DDD + domain events + CQRS ===");
var loan = Loan.Apply("CUST-7", new Money(25_000m, "USD"));
loan.Approve(creditScore: 720);
foreach (var ev in loan.DequeueEvents()) bus.Publish(ev);                    // after the write is committed
Console.WriteLine($"Query side: {readModel.Get(loan.Id)}");
try { new Money(-5m, "USD"); } catch (ArgumentException ex) { Console.WriteLine($"Value object guards invariants: {ex.Message}"); }

Console.WriteLine("\n=== Saga (orchestration) with compensation ===");
var ok = await new FundTransferSaga(new LedgerService(), new PaymentGateway(fail: false)).RunAsync("T-1", 500m);
Console.WriteLine($"Result: {ok}\n");
var failed = await new FundTransferSaga(new LedgerService(), new PaymentGateway(fail: true)).RunAsync("T-2", 500m);
Console.WriteLine($"Result: {failed}");

Console.WriteLine("\n=== Circuit breaker ===");
var breaker = new CircuitBreaker(failureThreshold: 3, openFor: TimeSpan.FromMilliseconds(300));
for (int i = 1; i <= 6; i++)
{
    try { await breaker.ExecuteAsync<string>(() => throw new HttpRequestException("credit bureau 503")); }
    catch (Exception ex) { Console.WriteLine($"call {i}: {ex.GetType().Name} – {ex.Message} (state {breaker.State})"); }
}
await Task.Delay(350);
var r = await breaker.ExecuteAsync(() => Task.FromResult("credit score 720"));
Console.WriteLine($"after cool-down: {r} (state {breaker.State})");

namespace Patterns
{
    // ---------- DDD ----------
    public sealed record Money
    {
        public decimal Amount { get; }
        public string Currency { get; }
        public Money(decimal amount, string currency)
        {
            if (amount < 0) throw new ArgumentException("Money cannot be negative");
            if (currency.Length != 3) throw new ArgumentException("ISO currency code required");
            (Amount, Currency) = (amount, currency);
        }
        public override string ToString() => $"{Amount:N2} {Currency}";
    }

    public interface IDomainEvent;
    public record LoanApproved(Guid LoanId, string CustomerId, Money Amount) : IDomainEvent;

    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _events = new();
        protected void Raise(IDomainEvent e) => _events.Add(e);
        public IReadOnlyList<IDomainEvent> DequeueEvents() { var copy = _events.ToList(); _events.Clear(); return copy; }
    }

    public sealed class Loan : AggregateRoot           // aggregate root: the only way to change a loan
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string CustomerId { get; }
        public Money Amount { get; }
        public string Status { get; private set; } = "Pending";
        private Loan(string customerId, Money amount) => (CustomerId, Amount) = (customerId, amount);

        public static Loan Apply(string customerId, Money amount) =>
            amount.Amount > 1_000_000 ? throw new InvalidOperationException("Over limit") : new Loan(customerId, amount);

        public void Approve(int creditScore)
        {
            if (Status != "Pending") throw new InvalidOperationException("Already decided");
            if (creditScore < 650) { Status = "Rejected"; return; }
            Status = "Approved";
            Raise(new LoanApproved(Id, CustomerId, Amount));
        }
    }

    // ---------- Event-driven + CQRS ----------
    public class InMemoryEventBus   // production: Azure Service Bus topics / Event Grid (with the Outbox, see 07)
    {
        private readonly Dictionary<Type, List<Action<object>>> _handlers = new();
        public void Subscribe<T>(Action<T> h) =>
            (_handlers.TryGetValue(typeof(T), out var l) ? l : _handlers[typeof(T)] = new()).Add(o => h((T)o));
        public void Publish(object e) { foreach (var h in _handlers.GetValueOrDefault(e.GetType()) ?? new()) h(e); }
    }

    public class LoanReadModel      // denormalized, query-optimised view (could be Cosmos DB / a reporting table)
    {
        private readonly Dictionary<Guid, string> _rows = new();
        public void On(LoanApproved e) => _rows[e.LoanId] = $"{e.CustomerId} | {e.Amount} | Approved";
        public string Get(Guid id) => _rows.GetValueOrDefault(id, "not found");
    }

    // ---------- Saga (orchestration) ----------
    public class LedgerService
    {
        public Task Reserve(string id, decimal amt) { Console.WriteLine($"  1. reserve {amt:C} in ledger"); return Task.CompletedTask; }
        public Task Release(string id) { Console.WriteLine("  ↩ compensate: release ledger reservation"); return Task.CompletedTask; }
        public Task Commit(string id) { Console.WriteLine("  3. commit ledger entry"); return Task.CompletedTask; }
    }
    public class PaymentGateway(bool fail)
    {
        public Task Send(string id, decimal amt)
        {
            Console.WriteLine($"  2. send payment {amt:C}");
            return fail ? Task.FromException(new InvalidOperationException("payment network rejected")) : Task.CompletedTask;
        }
    }

    public class FundTransferSaga(LedgerService ledger, PaymentGateway payments)
    {
        // Each step has a compensating action; no distributed transaction across services
        public async Task<string> RunAsync(string id, decimal amount)
        {
            var compensations = new Stack<Func<Task>>();
            try
            {
                await ledger.Reserve(id, amount); compensations.Push(() => ledger.Release(id));
                await payments.Send(id, amount);
                await ledger.Commit(id);
                return "Completed";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ {ex.Message}");
                while (compensations.Count > 0) await compensations.Pop()();
                return "Compensated (rolled back)";
            }
        }
    }

    // ---------- Resilience ----------
    public class CircuitBreaker(int failureThreshold, TimeSpan openFor)
    {
        private int _failures; private DateTime _openedAt;
        public string State { get; private set; } = "Closed";

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            if (State == "Open")
            {
                if (DateTime.UtcNow - _openedAt < openFor) throw new BrokenCircuitException("circuit open: failing fast");
                State = "Half-Open";                         // let one trial call through
            }
            try
            {
                var result = await action();
                _failures = 0; State = "Closed";
                return result;
            }
            catch when (State == "Half-Open" || ++_failures >= failureThreshold)
            {
                State = "Open"; _openedAt = DateTime.UtcNow;
                throw;
            }
        }
    }
    public class BrokenCircuitException(string m) : Exception(m);
}
