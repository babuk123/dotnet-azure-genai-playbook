namespace Domain;

// Domain: business rules only, no framework references
public sealed class Loan
{
    public Guid Id { get; } = Guid.NewGuid();
    public string CustomerId { get; }
    public decimal Amount { get; }
    public LoanStatus Status { get; private set; } = LoanStatus.Pending;

    public Loan(string customerId, decimal amount)
    {
        if (amount is <= 0 or > 1_000_000) throw new DomainException("Amount must be between 0 and 1,000,000");
        CustomerId = customerId; Amount = amount;
    }

    public void Approve(int creditScore)
    {
        if (Status != LoanStatus.Pending) throw new DomainException("Only pending loans can be approved");
        Status = creditScore >= 650 ? LoanStatus.Approved : LoanStatus.Rejected;
    }
}
public enum LoanStatus { Pending, Approved, Rejected }
public class DomainException(string message) : Exception(message);
