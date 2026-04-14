using Domain.Models;

namespace BankAccountSimulatorMVC.Interfaces;

public interface IBankAccountRepository
{
    Task<IReadOnlyList<BankAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task AddAsync(BankAccount account, CancellationToken cancellationToken = default);
}
