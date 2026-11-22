// EF Core 8 patterns for a banking API. Reference code (needs the EF Core NuGet packages above).
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<Account> Accounts { get; } = new();
}

public class Account
{
    public int Id { get; set; }
    public required string Number { get; set; }
    public decimal Balance { get; private set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public byte[] RowVersion { get; set; } = [];          // optimistic concurrency token
    public bool IsDeleted { get; set; }                   // soft delete

    public void Debit(decimal amount)
    {
        if (amount > Balance) throw new InvalidOperationException("Insufficient funds");
        Balance -= amount;
    }
    public void Credit(decimal amount) => Balance += amount;
}

public class BankingDbContext(DbContextOptions<BankingDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder b) =>
        b.ApplyConfigurationsFromAssembly(typeof(BankingDbContext).Assembly);
}

public class AccountConfig : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> e)
    {
        e.HasIndex(a => a.Number).IsUnique();
        e.Property(a => a.Number).HasMaxLength(20).IsRequired();
        e.Property(a => a.Balance).HasPrecision(18, 2);       // never float/double for money
        e.Property(a => a.RowVersion).IsRowVersion();
        e.HasQueryFilter(a => !a.IsDeleted);                  // global filter for soft delete
        e.HasOne(a => a.Customer).WithMany(c => c.Accounts).HasForeignKey(a => a.CustomerId);
    }
}

public class AccountQueries(BankingDbContext db)
{
    // Read-only query: AsNoTracking + projection = no change tracking, only the needed columns
    public Task<List<AccountSummary>> GetSummariesAsync(int customerId, CancellationToken ct) =>
        db.Accounts.AsNoTracking()
          .Where(a => a.CustomerId == customerId)
          .OrderBy(a => a.Number)
          .Select(a => new AccountSummary(a.Number, a.Balance))
          .ToListAsync(ct);

    // Avoid N+1: load the related data in one round trip (or use a projection)
    public Task<List<Customer>> CustomersWithAccountsAsync(CancellationToken ct) =>
        db.Customers.Include(c => c.Accounts).AsSplitQuery().ToListAsync(ct);

    // Compiled query for a very hot path
    private static readonly Func<BankingDbContext, string, Task<Account?>> ByNumber =
        EF.CompileAsyncQuery((BankingDbContext ctx, string number) => ctx.Accounts.FirstOrDefault(a => a.Number == number));
    public Task<Account?> FindAsync(string number) => ByNumber(db, number);

    // EF Core 7+: set-based update/delete without loading entities
    public Task<int> CloseDormantAsync(DateTime cutoff, CancellationToken ct) =>
        db.Accounts.Where(a => a.Balance == 0).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true), ct);
}

public class TransferService(BankingDbContext db)
{
    // Unit of work: both changes saved in one transaction by SaveChanges; RowVersion detects concurrent edits
    public async Task TransferAsync(string from, string to, decimal amount, CancellationToken ct)
    {
        for (int attempt = 1; ; attempt++)
        {
            var src = await db.Accounts.SingleAsync(a => a.Number == from, ct);
            var dst = await db.Accounts.SingleAsync(a => a.Number == to, ct);
            src.Debit(amount);
            dst.Credit(amount);
            try { await db.SaveChangesAsync(ct); return; }
            catch (DbUpdateConcurrencyException) when (attempt < 3)
            {
                db.ChangeTracker.Clear();   // someone else changed a balance: reload and retry
            }
        }
    }
}

public record AccountSummary(string Number, decimal Balance);

/* Program.cs
builder.Services.AddDbContextPool<BankingDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Db"),
                   sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));   // transient-fault retries for Azure SQL

Migrations:
  dotnet ef migrations add InitialCreate
  dotnet ef migrations script --idempotent -o migrate.sql   # review + run in the pipeline, not at app startup
*/
