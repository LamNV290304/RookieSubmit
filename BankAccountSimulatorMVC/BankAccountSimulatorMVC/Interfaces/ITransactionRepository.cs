using Domain.Models;

namespace BankAccountSimulatorMVC.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetHistoryAsync(string? accountNumber, TransactionType? filterType, CancellationToken cancellationToken = default);
    Task<decimal> GetTodayOutgoingTotalAsync(string accountNumber, DateTime date, CancellationToken cancellationToken = default);
}
