using Point = (int X, int Y);
// C# 12 / .NET 8 features (runs on .NET 8). See net10/ for C# 13/14 and .NET 9/10.
using System.Collections.Frozen;
using System.Globalization;

CultureInfo.CurrentCulture = new("en-US");

// Primary constructors (C# 12) for classes
var svc = new FeeService(new TimeProvider2Clock(TimeProvider.System));
Console.WriteLine($"Fee: {svc.Fee(1_000m):C}");

// Collection expressions + spread (C# 12)
int[] a = [1, 2, 3];
List<int> all = [.. a, 4, 5];
Console.WriteLine($"Collection expression: [{string.Join(", ", all)}]");

// FrozenDictionary (.NET 8): read-optimised lookup tables built once
var currencies = new Dictionary<string, string> { ["USD"] = "US Dollar", ["INR"] = "Indian Rupee" }.ToFrozenDictionary();
Console.WriteLine($"Frozen lookup: {currencies["INR"]}");

// TimeProvider (.NET 8): testable time without a custom IClock
var fake = new FixedTime(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));
Console.WriteLine($"Business hours at fixed time? {new FeeService(new TimeProvider2Clock(fake)).IsBusinessHours()}");

// Alias any type (C# 12)
Point p = (3, 4);
Console.WriteLine($"Alias any type: ({p.X}, {p.Y})");

// Default lambda parameters (C# 12)
var greet = (string name = "team") => $"Hello {name}";
Console.WriteLine(greet());

// Keyed services and IExceptionHandler are shown in 07 and 03.

public class TimeProvider2Clock(TimeProvider time) { public DateTimeOffset Now => time.GetUtcNow(); }
public class FeeService(TimeProvider2Clock clock)
{
    public decimal Fee(decimal amount) => Math.Max(5m, amount * 0.01m);
    public bool IsBusinessHours() => clock.Now.Hour is >= 9 and < 17;
}
public class FixedTime(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
