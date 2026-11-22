// PoC: AI-assisted vulnerability remediation for .NET dependencies.
// Runs offline: parses `dotnet list package --vulnerable --format json` output, prioritises findings,
// proposes the minimal safe upgrade, and drafts a PR description. In the real PoC an LLM step
// (Azure OpenAI) explains the CVE and checks release notes for breaking changes; a human reviews the PR.
using System.Text.Json;

// Sample scan output (package versions and advisory IDs are illustrative)
const string scanJson = """
{
  "projects": [{
    "path": "src/Orders.Api/Orders.Api.csproj",
    "frameworks": [{
      "framework": "net8.0",
      "topLevelPackages": [
        { "id": "System.Text.Json", "resolvedVersion": "8.0.3",
          "vulnerabilities": [{ "severity": "High", "advisoryurl": "https://github.com/advisories/GHSA-sample-0001" }] },
        { "id": "Azure.Identity", "resolvedVersion": "1.10.2",
          "vulnerabilities": [{ "severity": "Moderate", "advisoryurl": "https://github.com/advisories/GHSA-sample-0002" }] }
      ]
    }]
  }]
}
""";

// Fixed versions for the sample (the real tool looks these up in the GitHub Advisory Database / NuGet API)
var fixedIn = new Dictionary<string, string> { ["System.Text.Json"] = "8.0.5", ["Azure.Identity"] = "1.11.4" };
var rank = new Dictionary<string, int> { ["Critical"] = 0, ["High"] = 1, ["Moderate"] = 2, ["Low"] = 3 };

using var doc = JsonDocument.Parse(scanJson);
var findings =
    (from p in doc.RootElement.GetProperty("projects").EnumerateArray()
     from f in p.GetProperty("frameworks").EnumerateArray()
     from pkg in f.GetProperty("topLevelPackages").EnumerateArray()
     from v in pkg.GetProperty("vulnerabilities").EnumerateArray()
     select new Finding(p.GetProperty("path").GetString()!, pkg.GetProperty("id").GetString()!,
                        pkg.GetProperty("resolvedVersion").GetString()!, v.GetProperty("severity").GetString()!,
                        v.GetProperty("advisoryurl").GetString()!))
    .OrderBy(f => rank[f.Severity]).ToList();

Console.WriteLine("Prioritised findings:");
foreach (var f in findings)
{
    var target = fixedIn[f.Package];
    var risk = SemVerRisk(f.Version, target);
    Console.WriteLine($"  [{f.Severity}] {f.Package} {f.Version} → {target}  ({risk} upgrade)  {f.Advisory}");
}

Console.WriteLine("\nDraft PR description (for human review):\n");
Console.WriteLine("## Security: upgrade vulnerable NuGet packages");
foreach (var f in findings)
    Console.WriteLine($"- `{f.Package}` {f.Version} → {fixedIn[f.Package]} ({f.Severity}) – {f.Advisory}");
Console.WriteLine("\nChecklist: [ ] build  [ ] unit tests  [ ] `dotnet list package --vulnerable` clean\nReviewer: confirm no breaking changes in release notes before merge.");

static string SemVerRisk(string from, string to)
{
    var (a, b) = (Version.Parse(from), Version.Parse(to));
    return a.Major != b.Major ? "MAJOR (breaking changes likely)" : a.Minor != b.Minor ? "minor" : "patch";
}

record Finding(string Project, string Package, string Version, string Severity, string Advisory);
