// AI agent on an existing banking app: it plans, calls read-only tools, drafts a report,
// and a human (finance analyst) must approve before anything is published.
// The planner is scripted here so the loop runs offline; azure/ToolCallingAgent.cs uses a real model.
using System.Globalization;
using System.Text.Json;
using Agents;

CultureInfo.CurrentCulture = new("en-US");

var tools = new ToolRegistry()
    .Register("get_positions", "Read positions for an account", args =>
        JsonSerializer.Serialize(new[] { new { symbol = "MSFT", value = 120_000m }, new { symbol = "BND", value = 80_000m } }))
    .Register("get_drawdown", "Max drawdown % for an account", args => "12.5");

var agent = new ReportAgent(tools, new ScriptedPlanner());
var draft = await agent.RunAsync("Prepare the monthly risk summary for account ACC-1");

Console.WriteLine("\n--- DRAFT (pending human review) ---\n" + draft.Body);
var review = new HumanReview(approverRole: "FinanceAnalyst");
review.Approve(draft, approver: "fa.reviewer", comment: "Numbers match the source system");
Console.WriteLine($"\nStatus: {draft.Status} by {draft.ApprovedBy}");
Console.WriteLine("Audit trail:"); foreach (var a in draft.Audit) Console.WriteLine("  " + a);

namespace Agents
{
    public record ToolCall(string Name, string Args);
    public abstract record Step;
    public record CallTool(ToolCall Call) : Step;
    public record Finish(string Report) : Step;

    public interface IPlanner { Step Next(string goal, IReadOnlyList<(ToolCall Call, string Result)> history); }

    public class ToolRegistry
    {
        private readonly Dictionary<string, (string Description, Func<string, string> Run)> _tools = new();
        public ToolRegistry Register(string name, string description, Func<string, string> run) { _tools[name] = (description, run); return this; }
        // Allow-list: the model can only call registered, read-only tools
        public string Invoke(ToolCall c) => _tools.TryGetValue(c.Name, out var t) ? t.Run(c.Args) : throw new InvalidOperationException($"Tool {c.Name} not allowed");
    }

    public class ReportDraft(string body)
    {
        public string Body { get; } = body;
        public string Status { get; set; } = "PendingReview";
        public string? ApprovedBy { get; set; }
        public List<string> Audit { get; } = new();
    }

    public class ReportAgent(ToolRegistry tools, IPlanner planner, int maxSteps = 6)
    {
        public Task<ReportDraft> RunAsync(string goal)
        {
            var history = new List<(ToolCall, string)>();
            var audit = new List<string> { $"goal: {goal}" };
            for (int step = 1; step <= maxSteps; step++)          // hard step limit: no runaway loops
            {
                switch (planner.Next(goal, history))
                {
                    case CallTool(var call):
                        var result = tools.Invoke(call);
                        Console.WriteLine($"step {step}: {call.Name}({call.Args}) -> {result}");
                        audit.Add($"tool {call.Name}({call.Args})");
                        history.Add((call, result));
                        break;
                    case Finish(var report):
                        var draft = new ReportDraft(report);
                        draft.Audit.AddRange(audit); draft.Audit.Add("draft created");
                        return Task.FromResult(draft);
                }
            }
            throw new InvalidOperationException("Agent exceeded step limit");
        }
    }

    // Scripted stand-in for the LLM's decisions
    public class ScriptedPlanner : IPlanner
    {
        public Step Next(string goal, IReadOnlyList<(ToolCall Call, string Result)> h) => h.Count switch
        {
            0 => new CallTool(new ToolCall("get_positions", "{\"account\":\"ACC-1\"}")),
            1 => new CallTool(new ToolCall("get_drawdown", "{\"account\":\"ACC-1\"}")),
            _ => new Finish(BuildReport(h))
        };

        static string BuildReport(IReadOnlyList<(ToolCall Call, string Result)> h)
        {
            using var doc = JsonDocument.Parse(h[0].Result);
            var total = doc.RootElement.EnumerateArray().Sum(e => e.GetProperty("value").GetDecimal());
            return $"Account ACC-1 monthly risk summary\nTotal market value: {total:C0}\nMax drawdown: {h[1].Result}%\n(Generated from source-system data; requires analyst sign-off.)";
        }
    }

    public class HumanReview(string approverRole)
    {
        public void Approve(ReportDraft d, string approver, string comment)
        {
            d.Status = "Approved"; d.ApprovedBy = approver;
            d.Audit.Add($"approved by {approver} ({approverRole}): {comment}");
        }
    }
}
