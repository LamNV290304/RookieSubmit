using Domain.Models;
using BankAccountSimulatorMVC.Interfaces;
using BankAccountSimulatorMVC.Services.Interface;

namespace BankAccountSimulatorMVC.Services;

public class MonthlyInterestService(
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    ILogger<MonthlyInterestService> logger) : IMonthlyInterestService
{
    private readonly decimal _monthlyInterestRate = configuration.GetValue<decimal>("Config:MonthlyInterest", 0.02m);

    public async Task<ServiceResult> ApplyCurrentMonthAsync(CancellationToken cancellationToken = default)
    {
        if (_monthlyInterestRate <= 0) return ServiceResult.Fail("Monthly interest rate must be greater than 0.");

        var now = DateTime.UtcNow;
        var periodKey = $"{now:yyyy-MM}";

        var activeAccounts = await unitOfWork.BankAccounts.GetActiveForUpdateAsync(cancellationToken);
        if (activeAccounts.Count == 0) return ServiceResult.Ok("No active accounts to apply monthly interest.");

        var appliedCount = 0;
        await using var dbTransaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var account in activeAccounts)
            {
                var alreadyApplied = await unitOfWork.Transactions.HasMonthlyInterestAppliedAsync(
                    account.AccountNumber,
                    now.Year,
                    now.Month,
                    cancellationToken);

                if (alreadyApplied) continue;

                var interestAmount = Math.Round(account.Balance * _monthlyInterestRate, 2, MidpointRounding.AwayFromZero);
                if (interestAmount <= 0) continue;

                account.Balance += interestAmount;

                await unitOfWork.Transactions.AddAsync(
                    CreateInterestTransaction(account.AccountNumber, interestAmount, periodKey),
                    cancellationToken);

                appliedCount++;
            }

            if (appliedCount > 0) await unitOfWork.SaveChangesAsync(cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);
            return ServiceResult.Ok($"Monthly interest applied for {appliedCount} account(s).");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Monthly interest background task failed.");
            throw;
        }
    }

    private Transaction CreateInterestTransaction(string accountNumber, decimal amount, string periodKey)
    {
        return new Transaction
        {
            AccountNumber = accountNumber,
            Type = TransactionType.Deposit,
            Amount = amount,
            CreatedAt = DateTime.UtcNow,
            Description = $"Monthly interest {periodKey} ({_monthlyInterestRate:P2})"
        };
    }
}
