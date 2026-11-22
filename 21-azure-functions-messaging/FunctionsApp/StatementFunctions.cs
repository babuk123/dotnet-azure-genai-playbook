using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public record StatementRequest(string AccountId, string Month);

public class StatementFunctions(IStatementGenerator generator, ILogger<StatementFunctions> log)
{
    // 1. HTTP trigger: accept the request, queue it, return 202 (don't do long work inside HTTP)
    [Function("RequestStatement")]
    [ServiceBusOutput("statements", Connection = "ServiceBus")]
    public async Task<string> RequestStatement(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "statements")] HttpRequest req)
    {
        var body = await JsonSerializer.DeserializeAsync<StatementRequest>(req.Body)
                   ?? throw new BadHttpRequestException("Body required");
        log.LogInformation("Queued statement for {Account}", body.AccountId);
        return JsonSerializer.Serialize(body);    // returned string is sent to the Service Bus queue
    }

    // 2. Service Bus trigger: process messages; failures are retried, then dead-lettered after MaxDeliveryCount
    [Function("GenerateStatement")]
    public async Task GenerateStatement(
        [ServiceBusTrigger("statements", Connection = "ServiceBus")] ServiceBusReceivedMessage message,
        ServiceBusMessageActions actions, CancellationToken ct)
    {
        var request = message.Body.ToObjectFromJson<StatementRequest>()!;
        if (string.IsNullOrWhiteSpace(request.AccountId))
        {
            await actions.DeadLetterMessageAsync(message, deadLetterReason: "InvalidPayload", cancellationToken: ct);
            return;   // poison message: don't retry
        }
        var path = await generator.GenerateAsync(request.AccountId, ct);   // must be idempotent (at-least-once)
        log.LogInformation("Generated {Path} (delivery {Count})", path, message.DeliveryCount);
    }

    // 3. Timer trigger: nightly job at 02:00 UTC
    [Function("NightlyReconciliation")]
    public void Nightly([TimerTrigger("0 0 2 * * *")] TimerInfo timer) =>
        log.LogInformation("Reconciliation started; next run {Next}", timer.ScheduleStatus?.Next);
}
