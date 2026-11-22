using Application;
using Domain;
using Infrastructure;

// Api (composition root): wires the layers together
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ILoanRepository, InMemoryLoanRepository>();
builder.Services.AddSingleton<ICreditBureau, FakeCreditBureau>();
builder.Services.AddScoped<LoanService>();
var app = builder.Build();

app.MapPost("/api/loans", async (ApplyForLoan cmd, LoanService svc) =>
{
    try { return Results.Ok(await svc.ApplyAsync(cmd)); }
    catch (DomainException ex) { return Results.Problem(ex.Message, statusCode: 422); }
});
app.Run();
