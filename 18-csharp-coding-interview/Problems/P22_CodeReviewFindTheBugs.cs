namespace Interview;

// #22 Code review: find the bugs  [Advanced]
// Problem : Review this code and list every problem you see.
// Approach: Read line by line and explain the impact of each issue, not just the name.
// Complexity: n/a

// ---- Code given by the interviewer (DO NOT COPY) ----
// public class ReportService            // registered as AddSingleton
// {
//     private readonly AppDbContext _db; // AppDbContext is Scoped!
//     public ReportService(AppDbContext db) => _db = db;
//
//     public async void Generate(int id)                       // (2)
//     {
//         var client = new HttpClient();                       // (3)
//         var json = client.GetStringAsync(url + id).Result;   // (4)
//         var rows = _db.Rows.ToList().Where(r => r.Id == id); // (5)
//         foreach (var r in rows) Console.WriteLine(r.Amount); // (6)
//     }
// }
//
// ---- Problems to explain ----
// (1) Scoped DbContext captured by a Singleton -> shared across requests, not thread-safe.
//     Fix: make the service Scoped, or use IServiceScopeFactory / IDbContextFactory.
// (2) async void -> exceptions crash the process and callers cannot await. Use async Task.
// (3) new HttpClient per call -> socket exhaustion. Use IHttpClientFactory / typed client.
// (4) .Result blocks a thread (thread-pool starvation, possible deadlock). Use await.
// (5) ToList() before Where -> loads the whole table into memory. Filter in SQL first.
// (6) Console.WriteLine in a service -> use ILogger with structured logging.
// Also: no cancellation token, no error handling, no input validation.
