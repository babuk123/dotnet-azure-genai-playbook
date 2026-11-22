using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddSingleton<IStatementGenerator, StatementGenerator>();
builder.Build().Run();

public interface IStatementGenerator { Task<string> GenerateAsync(string accountId, CancellationToken ct); }
public class StatementGenerator : IStatementGenerator
{
    public Task<string> GenerateAsync(string accountId, CancellationToken ct) => Task.FromResult($"statements/{accountId}.pdf");
}
