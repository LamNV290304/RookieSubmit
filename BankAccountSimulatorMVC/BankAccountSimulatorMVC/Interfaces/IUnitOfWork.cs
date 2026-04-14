using Microsoft.EntityFrameworkCore.Storage;

namespace BankAccountSimulatorMVC.Interfaces;

public interface IUnitOfWork
{
    IBankAccountRepository BankAccounts { get; }
    ITransactionRepository Transactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
