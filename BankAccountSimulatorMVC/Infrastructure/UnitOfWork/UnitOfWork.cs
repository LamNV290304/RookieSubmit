using BankAccountSimulatorMVC.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.UnitOfWork;

public sealed class UnitOfWork(
    BankDbContext dbContext,
    IBankAccountRepository bankAccounts,
    ITransactionRepository transactions) : IUnitOfWork
{
    public IBankAccountRepository BankAccounts { get; } = bankAccounts;
    public ITransactionRepository Transactions { get; } = transactions;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.BeginTransactionAsync(cancellationToken);
}