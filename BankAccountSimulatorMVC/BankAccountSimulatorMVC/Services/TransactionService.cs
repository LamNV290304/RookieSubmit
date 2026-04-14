using Domain.Models;
using BankAccountSimulatorMVC.Interfaces;
using BankAccountSimulatorMVC.Services.Interface;

namespace BankAccountSimulatorMVC.Services;

public class TransactionService(IUnitOfWork unitOfWork, IConfiguration configuration) : ITransactionService
{
    private const decimal MinimumBalanceAfterWithdraw = 100m;
    private readonly decimal _dailyLimit = configuration.GetValue<decimal>("Config:DailyLimit", decimal.MaxValue);

    public async Task<ServiceResult> DepositAsync(string accountNumber, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return ServiceResult.Fail("Account number is required.");

        if (amount <= 0) return ServiceResult.Fail("Amount must be greater than 0.");

        var normalizedAccountNumber = accountNumber.Trim();
        var account = await unitOfWork.BankAccounts.GetByAccountNumberAsync(normalizedAccountNumber);
        if (account is null) return ServiceResult.Fail("Account not found.");

        if (account.Status != AccountStatus.Active) return ServiceResult.Fail("Account is frozen.");

        account.Balance += amount;

        await unitOfWork.Transactions.AddAsync(CreateTransaction(normalizedAccountNumber, TransactionType.Deposit, amount, "Cash deposit"));
        await unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok("Deposit completed.");
    }

    public async Task<ServiceResult> WithdrawAsync(string accountNumber, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return ServiceResult.Fail("Account number is required.");

        if (amount <= 0) return ServiceResult.Fail("Amount must be greater than 0.");

        var normalizedAccountNumber = accountNumber.Trim();
        var account = await unitOfWork.BankAccounts.GetByAccountNumberAsync(normalizedAccountNumber);
        if (account is null) return ServiceResult.Fail("Account not found.");

        if (account.Status != AccountStatus.Active) return ServiceResult.Fail("Account is frozen.");

        if (account.Balance - amount < MinimumBalanceAfterWithdraw) return ServiceResult.Fail($"Insufficient funds. Minimum balance after withdrawal is {MinimumBalanceAfterWithdraw:0.##}.");

        var todayOutgoing = await unitOfWork.Transactions.GetTodayOutgoingTotalAsync(normalizedAccountNumber, DateTime.UtcNow);
        if (todayOutgoing + amount > _dailyLimit) return ServiceResult.Fail($"Daily outgoing limit exceeded. Limit is {_dailyLimit:0.##}.");

        account.Balance -= amount;

        await unitOfWork.Transactions.AddAsync(CreateTransaction(normalizedAccountNumber, TransactionType.Withdraw, amount, "Cash withdrawal"));
        await unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok("Withdrawal completed.");
    }

    public async Task<ServiceResult> TransferAsync(string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(sourceAccountNumber)) return ServiceResult.Fail("Source account is required.");

        if (string.IsNullOrWhiteSpace(destinationAccountNumber)) return ServiceResult.Fail("Destination account is required.");

        if (amount <= 0) return ServiceResult.Fail("Amount must be greater than 0.");

        var source = sourceAccountNumber.Trim();
        var destination = destinationAccountNumber.Trim();

        if (source.Equals(destination, StringComparison.OrdinalIgnoreCase)) return ServiceResult.Fail("Source and destination account must be different.");

        var sourceAccount = await unitOfWork.BankAccounts.GetByAccountNumberAsync(source);
        if (sourceAccount is null) return ServiceResult.Fail("Source account not found.");

        var destinationAccount = await unitOfWork.BankAccounts.GetByAccountNumberAsync(destination);
        if (destinationAccount is null) return ServiceResult.Fail("Destination account not found.");

        if (sourceAccount.Status != AccountStatus.Active) return ServiceResult.Fail("Source account is frozen.");

        if (destinationAccount.Status != AccountStatus.Active) return ServiceResult.Fail("Destination account is frozen.");

        if (sourceAccount.Balance - amount < MinimumBalanceAfterWithdraw) return ServiceResult.Fail($"Insufficient funds. Minimum balance after transfer is {MinimumBalanceAfterWithdraw:0.##}.");

        var todayOutgoing = await unitOfWork.Transactions.GetTodayOutgoingTotalAsync(source, DateTime.UtcNow);
        if (todayOutgoing + amount > _dailyLimit) return ServiceResult.Fail($"Daily outgoing limit exceeded. Limit is {_dailyLimit:0.##}.");

        await using var dbTransaction = await unitOfWork.BeginTransactionAsync();
        try
        {
            sourceAccount.Balance -= amount;
            destinationAccount.Balance += amount;

            await unitOfWork.Transactions.AddAsync(CreateTransaction(source, TransactionType.Transfer, amount, $"Transfer out to {destination}"));
            await unitOfWork.Transactions.AddAsync(CreateTransaction(destination, TransactionType.Transfer, amount, $"Transfer in from {source}"));

            await unitOfWork.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return ServiceResult.Ok("Transfer completed.");
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<Transaction>> GetHistoryAsync(string? accountNumber, TransactionType? filterType)
    {
        var normalizedAccountNumber = string.IsNullOrWhiteSpace(accountNumber) ? null : accountNumber.Trim();
        return await unitOfWork.Transactions.GetHistoryAsync(normalizedAccountNumber, filterType);
    }

    private static Transaction CreateTransaction(string accountNumber, TransactionType type, decimal amount, string description)
    {
        return new Transaction
        {
            AccountNumber = accountNumber,
            Type = type,
            Amount = amount,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}
