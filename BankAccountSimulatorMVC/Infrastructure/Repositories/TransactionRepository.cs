using Domain.Models;
using BankAccountSimulatorMVC.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TransactionRepository(BankDbContext dbContext) : ITransactionRepository
{
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await dbContext.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<bool> HasMonthlyInterestAppliedAsync(string accountNumber, int year, int month, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        return await dbContext.Transactions
            .AsNoTracking()
            .AnyAsync(
                t => t.AccountNumber == accountNumber
                     && t.Type == TransactionType.Deposit
                     && t.CreatedAt >= start
                     && t.CreatedAt < end
                     && EF.Functions.Like(t.Description, "Monthly interest%"),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetHistoryAsync(string? accountNumber, TransactionType? filterType, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Transactions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(accountNumber)) query = query.Where(x => x.AccountNumber == accountNumber);

        if (filterType.HasValue) query = query.Where(x => x.Type == filterType.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTodayOutgoingTotalAsync(string accountNumber, DateTime date, CancellationToken cancellationToken = default)
    {
        var utcDate = date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };

        var start = utcDate.Date;
        var end = start.AddDays(1);

        var total = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.AccountNumber == accountNumber)
            .Where(t => t.CreatedAt >= start && t.CreatedAt < end)
            .Where(t => t.Type == TransactionType.Withdraw || (t.Type == TransactionType.Transfer && !EF.Functions.Like(t.Description, "Transfer in%")))
            .Select(t => (decimal?)t.Amount)
            .SumAsync(cancellationToken);

        return total ?? 0m;
    }
}