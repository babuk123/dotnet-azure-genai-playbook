// C# 13/14 and .NET 9/10 features. Build with the .NET 10 SDK (<TargetFramework>net10.0</TargetFramework>).
using System.Threading;

namespace WhatsNew;

// ---------- C# 14 (.NET 10): extension members (properties + static members) ----------
public static class AccountExtensions
{
    extension(Account account)
    {
        public bool IsOverdrawn => account.Balance < 0;          // extension PROPERTY
        public string Masked => "****" + account.Number[^4..];
    }
}

// ---------- C# 14: the `field` keyword in properties (no explicit backing field) ----------
public class Account
{
    public required string Number { get; init; }
    public decimal Balance
    {
        get;
        set => field = Math.Round(value, 2);                    // `field` = compiler-generated backing field
    }
}

public static class Demo
{
    // ---------- C# 14: null-conditional assignment ----------
    public static void Touch(Account? acc) => acc?.Balance = 0m;

    // ---------- C# 13: params collections (Span, List, IEnumerable…) ----------
    public static decimal Sum(params ReadOnlySpan<decimal> values)
    {
        decimal total = 0;
        foreach (var v in values) total += v;
        return total;
    }

    // ---------- C# 13 / .NET 9: System.Threading.Lock ----------
    private static readonly Lock _gate = new();
    private static int _counter;
    public static int Next() { lock (_gate) return ++_counter; }   // lock on the new Lock type uses its fast path

    // ---------- .NET 9 LINQ: CountBy / AggregateBy / Index ----------
    public static void Linq(IEnumerable<(string Branch, decimal Amount)> txns)
    {
        foreach (var (branch, count) in txns.CountBy(t => t.Branch))
            Console.WriteLine($"{branch}: {count} txns");
        foreach (var (branch, total) in txns.AggregateBy(t => t.Branch, 0m, (sum, t) => sum + t.Amount))
            Console.WriteLine($"{branch}: {total:C}");
        foreach (var (i, t) in txns.Index())
            Console.WriteLine($"#{i} {t.Branch}");
    }

    // ---------- .NET 9: Guid.CreateVersion7 (time-ordered IDs, index friendly) ----------
    public static Guid NewOrderId() => Guid.CreateVersion7();
}

/* ---------- ASP.NET Core 9/10 highlights (Program.cs) ----------
builder.Services.AddOpenApi();               // .NET 9: built-in OpenAPI document generation (Swashbuckle optional)
app.MapOpenApi();
builder.Services.AddHybridCache();           // .NET 9: L1 memory + L2 distributed cache with stampede protection
builder.Services.AddValidation();            // .NET 10: built-in validation for minimal API parameters
app.MapStaticAssets();                       // .NET 9: compressed, fingerprinted static files
// .NET 10: Server-Sent Events results (TypedResults.ServerSentEvents), passkey support in Identity,
//          file-based apps: `dotnet run app.cs` with no project file.
*/
