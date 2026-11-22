// C# 15 / .NET 11 (released Nov 10, 2026). Build with the .NET 11 SDK: <TargetFramework>net11.0</TargetFramework>.
// Excluded from this project's build because the repo targets .NET 10 (LTS).
namespace CSharp15;

// ---------- Union types: a value is exactly one of a fixed set of types ----------
public record class Approved(string LoanId, decimal Amount);
public record class Rejected(string LoanId, string Reason);
public record class NeedsReview(string LoanId, string Queue);

public union LoanDecision(Approved, Rejected, NeedsReview);

public static class Decisions
{
    // Exhaustive: no default arm needed; adding a 4th case makes the compiler flag every switch that misses it
    public static string Describe(LoanDecision decision) => decision switch
    {
        Approved a    => $"{a.LoanId} approved for {a.Amount:C}",
        Rejected r    => $"{r.LoanId} rejected: {r.Reason}",
        NeedsReview n => $"{n.LoanId} sent to {n.Queue}",
    };

    // A union can replace "Result<T>" libraries (OneOf etc.) for API/handler results
    public static LoanDecision Decide(string id, int score, decimal amount) =>
        score >= 700 ? new Approved(id, amount)
      : score >= 650 ? new NeedsReview(id, "credit-ops")
      :                new Rejected(id, "score below policy");
}

// ---------- Closed hierarchies: only the derived types declared here are allowed ----------
public closed record class PaymentState;
public record class Pending : PaymentState;
public record class Settled(DateTime At) : PaymentState;
public record class Failed(string Code) : PaymentState;

public static class Payments
{
    public static string Show(PaymentState s) => s switch
    {
        Pending          => "pending",
        Settled(var at)  => $"settled {at:d}",
        Failed(var code) => $"failed ({code})",
        // no warning: every direct descendant of PaymentState is handled
    };
}

// ---------- Collection expression arguments: with(...) passes constructor arguments ----------
public static class Collections
{
    public static void Demo(string[] accountIds)
    {
        List<string> ids = [with(capacity: accountIds.Length * 2), .. accountIds];
        HashSet<string> codes = [with(StringComparer.OrdinalIgnoreCase), "usd", "USD", "Usd"];   // 1 element
        Console.WriteLine($"{ids.Count} ids, {codes.Count} currency code(s)");
    }
}

// ---------- Extension indexers (builds on C# 14 extension members) ----------
public static class SequenceIndexer
{
    extension(IEnumerable<decimal> amounts)
    {
        public decimal this[int index] => amounts.ElementAt(index);
    }
}

// ---------- Labeled break / continue: leave an outer loop directly ----------
public static class Matrix
{
    public static (int Row, int Col)? FindFirstOverLimit(decimal[,] exposures, decimal limit)
    {
        (int, int)? hit = null;
        outer: for (int r = 0; r < exposures.GetLength(0); r++)
        {
            for (int c = 0; c < exposures.GetLength(1); c++)
            {
                if (exposures[r, c] < 0) continue outer;          // skip rows with bad data
                if (exposures[r, c] > limit) { hit = (r, c); break outer; }
            }
        }
        return hit;
    }
}

/* ---------- ASP.NET Core 11 highlights ----------
- Async validation for minimal APIs (e.g. "is this account number unique?" checks that call a database)
- OpenAPI 3.2 documents by default (listed as a breaking change: check client generators)
- Automatic cross-origin (CSRF) protection; short-circuit endpoints with an attribute
- Union types supported in minimal API results
- SignalR authentication refresh without reconnecting; cancel hub invocations from the client
- Blazor AI components (experimental) using AG-UI for agentic UIs
.NET 11 platform:
- Runtime-native async ("runtime async"), Zstandard compression built in, Native AOT CLI host
- SDK: `dotnet new` MCP server template; vulnerability and end-of-life checks during build
*/
