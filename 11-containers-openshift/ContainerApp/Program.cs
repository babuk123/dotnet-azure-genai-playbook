// A container-ready ASP.NET Core 8 app: config from env vars, health probes,
// graceful shutdown, structured console logs, and a page showing what the runtime sees inside the container.
using System.Runtime.InteropServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// 1. Logs: write JSON to stdout; the platform (Docker/OpenShift/AKS) collects them
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);

// 2. Shutdown: give in-flight requests time to finish after SIGTERM
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(25));

// 3. Health checks: liveness = process alive; readiness = dependencies OK and not shutting down
builder.Services.AddSingleton<ReadinessState>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<ReadinessCheck>("ready", tags: ["ready"]);

builder.Services.AddHostedService<QueueWorker>();

var app = builder.Build();

var lifetime = app.Lifetime;
var ready = app.Services.GetRequiredService<ReadinessState>();
lifetime.ApplicationStarted.Register(() => { ready.IsReady = true; app.Logger.LogInformation("Started; ready for traffic"); });
lifetime.ApplicationStopping.Register(() =>
{
    ready.IsReady = false;   // readiness fails first so the load balancer stops sending traffic
    app.Logger.LogInformation("SIGTERM received; draining in-flight requests");
});

app.MapHealthChecks("/health/live", new() { Predicate = c => c.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

// What .NET sees inside the container (CPU/memory limits are container-aware since .NET Core 3.0)
app.MapGet("/runtime", (IConfiguration config) => new
{
    Framework = RuntimeInformation.FrameworkDescription,
    Os = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.OSArchitecture.ToString(),
    InContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true",
    ProcessorCount = Environment.ProcessorCount,                       // respects --cpus limit
    GcHeapLimitMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024,  // respects --memory limit
    ServerGc = System.Runtime.GCSettings.IsServerGC,
    User = Environment.UserName,
    Environment = builder.Environment.EnvironmentName,
    Greeting = config["App:Greeting"] ?? "(set App__Greeting env var)"   // App__Greeting -> App:Greeting
});

app.MapGet("/slow", async (CancellationToken ct) =>
{
    await Task.Delay(TimeSpan.FromSeconds(5), ct);   // try `docker stop` during this call: it still completes
    return "finished";
});

app.Run();

public class ReadinessState { public volatile bool IsReady; }

public class ReadinessCheck(ReadinessState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        Task.FromResult(state.IsReady ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("not ready / shutting down"));
}

// Background work must also stop cleanly on SIGTERM
public class QueueWorker(ILogger<QueueWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try { while (await timer.WaitForNextTickAsync(stoppingToken)) log.LogInformation("Processed queue batch"); }
        catch (OperationCanceledException) { log.LogInformation("Worker stopped cleanly"); }
    }
}
