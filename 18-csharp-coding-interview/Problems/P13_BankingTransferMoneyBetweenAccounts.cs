namespace Interview;

// #13 Banking: transfer money between accounts (validation + thread safety)  [Intermediate]
// Problem : Implement Transfer(fromId, toId, amount) and a search for transfers by account and date range. Rules: amount > 0, accounts exist, no overdraft, no transfer to the same account, and it must be safe when called from many threads.
// Approach: Validate first; lock both accounts in a consistent order (by Id) to avoid deadlocks; record the transfer; return a result object instead of throwing for business errors.
// Complexity: Transfer O(1); search O(n) in memory (in a real system: indexed DB query).

public class Account
{
    public required string Id { get; init; }
    public decimal Balance { get; set; }
    public object Sync { get; } = new();
}

public record Transfer(Guid Id, string From, string To, decimal Amount, DateTime At);
public record TransferResult(bool Success, string? Error = null, Transfer? Transfer = null);

public class TransferService(Dictionary<string, Account> accounts)
{
    private readonly List<Transfer> _history = new();
    private readonly object _historyLock = new();

    public TransferResult CreateTransfer(string fromId, string toId, decimal amount)
    {
        if (amount <= 0) return new(false, "Amount must be positive");
        if (fromId == toId) return new(false, "Cannot transfer to the same account");
        if (!accounts.TryGetValue(fromId, out var from)) return new(false, "Source account not found");
        if (!accounts.TryGetValue(toId, out var to)) return new(false, "Target account not found");

        // Always lock in the same order to avoid deadlocks
        var (first, second) = string.CompareOrdinal(from.Id, to.Id) < 0 ? (from, to) : (to, from);
        lock (first.Sync)
        lock (second.Sync)
        {
            if (from.Balance < amount) return new(false, "Insufficient funds");
            from.Balance -= amount;
            to.Balance += amount;
        }

        var t = new Transfer(Guid.NewGuid(), fromId, toId, amount, DateTime.UtcNow);
        lock (_historyLock) _history.Add(t);
        return new(true, Transfer: t);
    }

    public List<Transfer> SearchTransfers(string accountId, DateTime fromDate, DateTime toDate)
    {
        lock (_historyLock)
            return _history.Where(t => (t.From == accountId || t.To == accountId)
                                    && t.At >= fromDate && t.At <= toDate)
                           .OrderByDescending(t => t.At)
                           .ToList();
    }
}
