// Simplified customer onboarding pipeline: CIP → sanctions screening → risk rating → CDD/EDD → party creation.
// Shows idempotency (no duplicate parties on retries) and an audit trail. Generic example, no real data.
using System.Collections.Concurrent;

var onboarding = new OnboardingService(new SanctionsScreening(), new RiskEngine());

var apps = new[]
{
    new Application("APP-1001", "Asha Rao",     "US", "Engineer",  SourceOfFunds: "Salary",       ExpectedMonthly: 8_000m),
    new Application("APP-1002", "Ivan Petrov",  "US", "Importer",  SourceOfFunds: "Business",     ExpectedMonthly: 95_000m),
    new Application("APP-1003", "Blocked Name", "US", "Consultant",SourceOfFunds: "Salary",       ExpectedMonthly: 5_000m),
};

foreach (var app in apps)
    Print(onboarding.Onboard(app));

Console.WriteLine("\nClient retries APP-1001 (timeout on their side) 5 times in parallel:");
var retries = Enumerable.Range(0, 5).Select(_ => Task.Run(() => onboarding.Onboard(apps[0]))).ToArray();
Task.WaitAll(retries);
Console.WriteLine($"distinct party ids returned: {retries.Select(r => r.Result.PartyId).Distinct().Count()} (expected 1)");
Console.WriteLine($"parties created in core banking: {onboarding.PartiesCreated} (expected 1: retries created no duplicates)");

Console.WriteLine("\nAudit trail:");
foreach (var e in onboarding.Audit) Console.WriteLine($"  {e}");

static void Print(OnboardingResult r) =>
    Console.WriteLine($"{r.ApplicationId}: {r.Status,-14} risk={r.Risk,-6} party={r.PartyId ?? "-"} {r.Reason}");

public record Application(string Id, string Name, string Country, string Occupation, string SourceOfFunds, decimal ExpectedMonthly);
public enum RiskLevel { Low, Medium, High }
public record OnboardingResult(string ApplicationId, string Status, RiskLevel Risk, string? PartyId, string Reason = "");

public class SanctionsScreening
{
    private static readonly HashSet<string> Watchlist = new(StringComparer.OrdinalIgnoreCase) { "Blocked Name" };
    public bool IsHit(string name) => Watchlist.Contains(name);   // real systems use fuzzy matching vendors
}

public class RiskEngine
{
    public RiskLevel Rate(Application a) =>
        a.ExpectedMonthly > 50_000m || a.SourceOfFunds == "Business" ? RiskLevel.High
        : a.ExpectedMonthly > 20_000m ? RiskLevel.Medium
        : RiskLevel.Low;
}

public class OnboardingService(SanctionsScreening screening, RiskEngine risk)
{
    private readonly ConcurrentDictionary<string, Lazy<OnboardingResult>> _processed = new();  // idempotency
    private int _parties;
    public int PartiesCreated => _parties;
    public ConcurrentQueue<string> Audit { get; } = new();

    public OnboardingResult Onboard(Application app) =>
        _processed.GetOrAdd(app.Id, _ => new Lazy<OnboardingResult>(() => Process(app))).Value;

    private OnboardingResult Process(Application app)
    {
        Log(app.Id, "CIP identity verified");                       // 1. Customer Identification Program
        if (screening.IsHit(app.Name))                              // 2. sanctions / PEP screening
        {
            Log(app.Id, "Sanctions hit -> routed to compliance review");
            return new(app.Id, "Manual review", RiskLevel.High, null, "(possible sanctions match)");
        }
        var level = risk.Rate(app);                                 // 3. risk rating
        Log(app.Id, $"Risk rated {level}");
        if (level == RiskLevel.High)                                // 4. EDD for high risk
        {
            Log(app.Id, "Enhanced due diligence required");
            return new(app.Id, "EDD pending", level, null, "(needs source-of-funds documents)");
        }
        var partyId = $"PTY-{Interlocked.Increment(ref _parties):D6}"; // 5. create party once
        Log(app.Id, $"Party {partyId} created");
        return new(app.Id, "Approved", level, partyId);
    }

    private void Log(string appId, string msg) => Audit.Enqueue($"{DateTime.UtcNow:HH:mm:ss.fff} {appId} {msg}");
}
