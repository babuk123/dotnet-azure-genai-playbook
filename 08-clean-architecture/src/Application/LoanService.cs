using Domain;
namespace Application;

// Application: use cases + ports (interfaces) the outer layers implement
public interface ILoanRepository { Task AddAsync(Loan loan); Task<Loan?> GetAsync(Guid id); }
public interface ICreditBureau { Task<int> GetScoreAsync(string customerId); }

public record ApplyForLoan(string CustomerId, decimal Amount);
public record LoanDto(Guid Id, string Status);

public class LoanService(ILoanRepository repo, ICreditBureau bureau)
{
    public async Task<LoanDto> ApplyAsync(ApplyForLoan cmd)
    {
        var loan = new Loan(cmd.CustomerId, cmd.Amount);
        loan.Approve(await bureau.GetScoreAsync(cmd.CustomerId));
        await repo.AddAsync(loan);
        return new LoanDto(loan.Id, loan.Status.ToString());
    }
}
