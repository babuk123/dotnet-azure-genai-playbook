// The same "drawdown report" operation as the legacy WCF service, re-implemented as a modern REST API.
// Contract stays equivalent (same fields), faults become ProblemDetails, and it runs on Linux/containers.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IDrawdownCalculator, DrawdownCalculator>();
var app = builder.Build();

app.MapPost("/api/v1/reports/drawdown", (ReportRequest req, IDrawdownCalculator calc) =>
{
    if (string.IsNullOrWhiteSpace(req.AccountId))
        return Results.Problem(title: "Invalid request", detail: "AccountId required",
                               statusCode: 400, extensions: new Dictionary<string, object?> { ["code"] = "E100" });
    return Results.Ok(new ReportResponse(req.AccountId, calc.MaxDrawdownPercent(SampleValues())));
});

app.Run();

static decimal[] SampleValues() => [100m, 112m, 105m, 98m, 120m, 108m];

public record ReportRequest(string AccountId, DateOnly AsOfDate);
public record ReportResponse(string AccountId, decimal MaxDrawdownPercent);

public interface IDrawdownCalculator { decimal MaxDrawdownPercent(IReadOnlyList<decimal> values); }

// Max drawdown = largest peak-to-trough fall, in percent
public class DrawdownCalculator : IDrawdownCalculator
{
    public decimal MaxDrawdownPercent(IReadOnlyList<decimal> values)
    {
        decimal peak = values[0], maxDd = 0;
        foreach (var v in values)
        {
            peak = Math.Max(peak, v);
            maxDd = Math.Max(maxDd, (peak - v) / peak);
        }
        return Math.Round(maxDd * 100, 2);
    }
}
