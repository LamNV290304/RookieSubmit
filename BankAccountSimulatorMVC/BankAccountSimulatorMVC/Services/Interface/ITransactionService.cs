using Domain.Models;
using BankAccountSimulatorMVC.Services;

namespace BankAccountSimulatorMVC.Services.Interface;

public interface ITransactionService
{
    Task<ServiceResult> DepositAsync(string accountNumber, decimal amount);
    Task<ServiceResult> WithdrawAsync(string accountNumber, decimal amount);
    Task<ServiceResult> TransferAsync(string sourceAccountNumber, string destinationAccountNumber, decimal amount);
    Task<IReadOnlyList<Transaction>> GetHistoryAsync(string? accountNumber, TransactionType? filterType);
}
