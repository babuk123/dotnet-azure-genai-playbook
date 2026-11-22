namespace Interview;

// #16 Fix the N+1 query problem (EF Core)  [Intermediate]
// Problem : This code runs one query for orders and then one query per order for its customer. Fix it.
// Approach: Load related data in the same query: Include, or better for reads, project only needed columns with Select. Add AsNoTracking for read-only queries.
// Complexity: From 1 + N round trips to 1.

// BAD: lazy loading / per-row queries -> 1 + N round trips
// foreach (var o in db.Orders.ToList())
//     Console.WriteLine(o.Customer.Name);   // each access hits the DB

// GOOD 1: eager loading
// var orders = await db.Orders
//     .Include(o => o.Customer)
//     .AsNoTracking()
//     .ToListAsync();

// GOOD 2 (best for read APIs): projection - one SQL query, only needed columns
// var dto = await db.Orders
//     .Where(o => o.CreatedAt >= since)
//     .Select(o => new OrderDto(o.Id, o.Total, o.Customer.Name))
//     .AsNoTracking()
//     .ToListAsync();

public record OrderDto(int Id, decimal Total, string CustomerName);
