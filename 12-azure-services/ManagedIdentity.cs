// Using managed identity from .NET (packages: Azure.Identity, Azure.Security.KeyVault.Secrets,
// Azure.Messaging.ServiceBus, Azure.Extensions.AspNetCore.Configuration.Secrets). Reference only.
using Azure.Identity;
using Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);
var credential = new DefaultAzureCredential();   // managed identity in Azure, your az login locally

// Key Vault secrets become normal configuration values
builder.Configuration.AddAzureKeyVault(new Uri(builder.Configuration["KeyVaultUri"]!), credential);

builder.Services.AddSingleton(_ =>
    new ServiceBusClient(builder.Configuration["ServiceBus:FullyQualifiedNamespace"], credential));

var app = builder.Build();
app.MapPost("/orders", async (Order order, ServiceBusClient bus) =>
{
    await using var sender = bus.CreateSender("orders");
    await sender.SendMessageAsync(new ServiceBusMessage(BinaryData.FromObjectAsJson(order))
    {
        MessageId = order.Id   // duplicate detection / idempotent consumers
    });
    return Results.Accepted();
});
app.Run();

record Order(string Id, decimal Amount);
