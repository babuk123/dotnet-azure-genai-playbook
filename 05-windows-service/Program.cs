// .NET 8 Worker Service that runs as a Windows Service (and as a console app while developing).
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(o => o.ServiceName = "Report Delivery Worker");
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.AddSingleton<IReportQueue, InMemoryReportQueue>();
builder.Services.AddHostedService<ReportDeliveryWorker>();

builder.Build().Run();

public class WorkerOptions { public int PollSeconds { get; set; } = 10; public int BatchSize { get; set; } = 50; }

public interface IReportQueue { Task<IReadOnlyList<string>> DequeueBatchAsync(int size, CancellationToken ct); }

public class InMemoryReportQueue : IReportQueue
{
    private int _n;
    public Task<IReadOnlyList<string>> DequeueBatchAsync(int size, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(Enumerable.Range(0, 3).Select(_ => $"REPORT-{Interlocked.Increment(ref _n):D5}").ToList());
}

public class ReportDeliveryWorker(IReportQueue queue, Microsoft.Extensions.Options.IOptions<WorkerOptions> options,
                                  ILogger<ReportDeliveryWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.Value;
        log.LogInformation("Worker started, polling every {Seconds}s", opt.PollSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(opt.PollSeconds));
        do
        {
            try
            {
                var batch = await queue.DequeueBatchAsync(opt.BatchSize, stoppingToken);
                foreach (var report in batch)
                    log.LogInformation("Delivered {Report}", report);   // email / feed / archive
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Batch failed; will retry next cycle");   // never let the service die silently
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
        log.LogInformation("Worker stopping gracefully");
    }
}
