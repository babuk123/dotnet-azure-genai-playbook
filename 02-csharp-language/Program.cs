// Modern C# (12) features used in day-to-day enterprise code.
using System.Diagnostics;
using System.Globalization;

CultureInfo.CurrentCulture = new CultureInfo("en-US");   // consistent currency formatting

var accounts = new List<Account>
{
    new("ACC-1", "Asha",  AccountType.Checking, 2_500m),
    new("ACC-2", "Ben",   AccountType.Savings,  12_000m),
    new("ACC-3", "Cara",  AccountType.Brokerage, 250_000m),
    new("ACC-4", "Dev",   AccountType.Savings,  -50m),
};

Console.WriteLine("=== Records: value equality + with-expressions ===");
var a1 = accounts[0];
var a2 = a1 with { Balance = 3_000m };
Console.WriteLine($"{a1}\n{a2}\nsame identity? {ReferenceEquals(a1, a2)}, equal values? {a1 == a1 with { }}");

Console.WriteLine("\n=== Pattern matching (switch expression) ===");
foreach (var acc in accounts)
    Console.WriteLine($"{acc.Id}: {Classify(acc)}");

static string Classify(Account a) => a switch
{
    { Balance: < 0 } => "OVERDRAWN - alert",
    { Type: AccountType.Brokerage, Balance: >= 100_000m } => "Wealth client",
    { Type: AccountType.Savings } s when s.Balance > 10_000m => "Premium savings",
    _ => "Standard",
};

Console.WriteLine("\n=== LINQ: group, aggregate, project ===");
var byType = accounts
    .GroupBy(a => a.Type)
    .Select(g => new { Type = g.Key, Count = g.Count(), Total = g.Sum(a => a.Balance) })
    .OrderByDescending(x => x.Total);
foreach (var row in byType) Console.WriteLine($"{row.Type,-10} count={row.Count} total={row.Total:C0}");

Console.WriteLine("\n=== Deferred execution ===");
int evaluations = 0;
var query = accounts.Where(a => { evaluations++; return a.Balance > 0; });   // nothing runs yet
Console.WriteLine($"after defining query: {evaluations} evaluations");
_ = query.Count(); _ = query.Count();                                          // runs twice!
Console.WriteLine($"after enumerating twice: {evaluations} evaluations  -> materialize with ToList() if reused");

Console.WriteLine("\n=== async/await: sequential vs concurrent ===");
var sw = Stopwatch.StartNew();
await GetRateAsync("USD"); await GetRateAsync("EUR"); await GetRateAsync("INR");
Console.WriteLine($"sequential: {sw.ElapsedMilliseconds} ms");
sw.Restart();
var rates = await Task.WhenAll(GetRateAsync("USD"), GetRateAsync("EUR"), GetRateAsync("INR"));
Console.WriteLine($"Task.WhenAll: {sw.ElapsedMilliseconds} ms -> {string.Join(", ", rates)}");

Console.WriteLine("\n=== Span<T>: parse without allocating substrings ===");
var (parsedId, parsedAmount) = ParseFeedLine("ACC-3|Brokerage|250000.00");
Console.WriteLine($"id={parsedId} amount={parsedAmount:N2}");

// Span<T> lives only on the stack, so it cannot be declared inside an async method
// (top-level code here is async because of await) -> keep span logic in a sync method.
static (string Id, decimal Amount) ParseFeedLine(string text)
{
    ReadOnlySpan<char> line = text;
    ReadOnlySpan<char> id = line[..line.IndexOf('|')];
    ReadOnlySpan<char> amount = line[(line.LastIndexOf('|') + 1)..];
    return (id.ToString(), decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));
}

Console.WriteLine("\n=== Nullable reference types ===");
string? maybeName = accounts.FirstOrDefault(a => a.Id == "ACC-9")?.Owner;
Console.WriteLine($"owner: {maybeName ?? "(not found)"}");

static async Task<string> GetRateAsync(string currency)
{
    await Task.Delay(300);            // simulated HTTP call
    return $"{currency}=ok";
}

public enum AccountType { Checking, Savings, Brokerage }
public record Account(string Id, string Owner, AccountType Type, decimal Balance);
