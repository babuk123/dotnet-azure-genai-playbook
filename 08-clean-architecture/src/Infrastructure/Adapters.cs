using System.Collections.Concurrent;
using Application;
using Domain;
namespace Infrastructure;

// Infrastructure: adapters for the ports (swap for EF Core / real HTTP client in production)
public class InMemoryLoanRepository : ILoanRepository
{
    private readonly ConcurrentDictionary<Guid, Loan> _store = new();
    public Task AddAsync(Loan loan) { _store[loan.Id] = loan; return Task.CompletedTask; }
    public Task<Loan?> GetAsync(Guid id) => Task.FromResult(_store.GetValueOrDefault(id));
}
public class FakeCreditBureau : ICreditBureau
{
    public Task<int> GetScoreAsync(string customerId) => Task.FromResult(customerId.StartsWith("GOOD") ? 760 : 580);
}
