// ASP.NET Core 8 minimal API showing production patterns using only built-in features.
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();                       // RFC 7807 error responses
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddOutputCache();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("per-client", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 0;
    });
});
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddSingleton<IdempotencyStore>();

var app = builder.Build();

// Middleware order matters: errors first, then rate limiting/caching, then endpoints
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.UseOutputCache();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

var orders = app.MapGroup("/api/v1/orders")
                .WithTags("Orders")
                .RequireRateLimiting("per-client");

// GET list with paging (cached for 10 s)
orders.MapGet("/", (IOrderRepository repo, int page = 1, int pageSize = 10) =>
{
    pageSize = Math.Clamp(pageSize, 1, 100);
    var items = repo.GetAll().Skip((page - 1) * pageSize).Take(pageSize).ToList();
    return TypedResults.Ok(new PagedResult<Order>(items, page, pageSize, repo.GetAll().Count));
}).CacheOutput(p => p.Expire(TimeSpan.FromSeconds(10)));

// GET by id -> 200 or 404
orders.MapGet("/{id:int}", Results<Ok<Order>, NotFound> (int id, IOrderRepository repo) =>
    repo.Get(id) is { } order ? TypedResults.Ok(order) : TypedResults.NotFound());

// POST with validation + Idempotency-Key header -> 201 Created (or the stored result on retry)
orders.MapPost("/", async Task<IResult> (CreateOrderRequest req, HttpRequest http,
                                        IOrderRepository repo, IdempotencyStore idem) =>
{
    var errors = Validate(req);
    if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

    var key = http.Headers["Idempotency-Key"].ToString();
    if (string.IsNullOrWhiteSpace(key))
        return TypedResults.Problem("Idempotency-Key header is required", statusCode: 400);

    var order = await idem.GetOrCreateAsync(key, () => Task.FromResult(repo.Add(req)));
    return TypedResults.Created($"/api/v1/orders/{order.Id}", order);
});

// DELETE -> 204
orders.MapDelete("/{id:int}", Results<NoContent, NotFound> (int id, IOrderRepository repo) =>
    repo.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound());

// Endpoint that throws -> handled globally as ProblemDetails (no stack trace leaked)
app.MapGet("/api/v1/boom", () => { throw new InvalidOperationException("Something failed"); });

app.Run();

static Dictionary<string, string[]> Validate(object model)
{
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
    return results.GroupBy(r => r.MemberNames.FirstOrDefault() ?? "")
                  .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Invalid").ToArray());
}

// ----- models -----
public record Order(int Id, string CustomerId, decimal Amount, DateTime CreatedAt);
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public class CreateOrderRequest
{
    [Required, StringLength(20, MinimumLength = 3)] public string CustomerId { get; set; } = "";
    [Range(0.01, 1_000_000)] public decimal Amount { get; set; }
}

// ----- services -----
public interface IOrderRepository
{
    IReadOnlyList<Order> GetAll();
    Order? Get(int id);
    Order Add(CreateOrderRequest req);
    bool Remove(int id);
}

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private int _nextId;
    public IReadOnlyList<Order> GetAll() => _orders.Values.OrderBy(o => o.Id).ToList();
    public Order? Get(int id) => _orders.GetValueOrDefault(id);
    public Order Add(CreateOrderRequest r)
    {
        var o = new Order(Interlocked.Increment(ref _nextId), r.CustomerId, r.Amount, DateTime.UtcNow);
        _orders[o.Id] = o;
        return o;
    }
    public bool Remove(int id) => _orders.TryRemove(id, out _);
}

// Ensures a retried POST with the same key creates the order only once (even concurrently)
public class IdempotencyStore
{
    private readonly ConcurrentDictionary<string, Lazy<Task<Order>>> _store = new();
    public Task<Order> GetOrCreateAsync(string key, Func<Task<Order>> create) =>
        _store.GetOrAdd(key, _ => new Lazy<Task<Order>>(create)).Value;
}

// Converts unhandled exceptions into a ProblemDetails response with a trace id
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log, IProblemDetailsService problems)
    : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        log.LogError(ex, "Unhandled error");
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = { Title = "An unexpected error occurred", Status = 500 },
        });
    }
}
