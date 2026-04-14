using Domain.Models;
using BankAccountSimulatorMVC.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BankAccountRepository(BankDbContext dbContext) : IBankAccountRepository
{
    public async Task<IReadOnlyList<BankAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.BankAccounts
            .AsNoTracking()
            .OrderBy(x => x.AccountNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await dbContext.BankAccounts
            .FirstOrDefaultAsync(x => x.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task AddAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        await dbContext.BankAccounts.AddAsync(account, cancellationToken);
    }
}