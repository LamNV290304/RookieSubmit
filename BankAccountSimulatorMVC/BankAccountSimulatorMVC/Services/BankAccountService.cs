using Domain.Models;
using BankAccountSimulatorMVC.Interfaces;
using BankAccountSimulatorMVC.Services.Interface;

namespace BankAccountSimulatorMVC.Services;

public class BankAccountService(IUnitOfWork unitOfWork) : IBankAccountService
{
    public async Task<ServiceResult> CreateAsync(BankAccount account)
    {
        if (string.IsNullOrWhiteSpace(account.AccountNumber)) return ServiceResult.Fail("Account number is required.");

        var normalizedAccountNumber = account.AccountNumber.Trim();
        var existing = await unitOfWork.BankAccounts.GetByAccountNumberAsync(normalizedAccountNumber);
        if (existing is not null) return ServiceResult.Fail("Account number already exists.");

        account.AccountNumber = normalizedAccountNumber;

        await unitOfWork.BankAccounts.AddAsync(account);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok("Account created successfully.");
    }

    public Task<IReadOnlyList<BankAccount>> GetAllAsync()
    {
        return unitOfWork.BankAccounts.GetAllAsync();
    }

    public Task<BankAccount?> GetByAccountNumberAsync(string accountNumber)
    {
        return unitOfWork.BankAccounts.GetByAccountNumberAsync(accountNumber);
    }

    public Task<ServiceResult> FreezeAsync(string accountNumber)
    {
        return UpdateAccountStatusAsync(
            accountNumber,
            AccountStatus.Frozen,
            "Account is already frozen.",
            "Account frozen successfully.");
    }

    public Task<ServiceResult> UnfreezeAsync(string accountNumber)
    {
        return UpdateAccountStatusAsync(
            accountNumber,
            AccountStatus.Active,
            "Account is already active.",
            "Account unfrozen successfully.");
    }

    private async Task<ServiceResult> UpdateAccountStatusAsync(
        string accountNumber,
        AccountStatus targetStatus,
        string alreadyInStatusMessage,
        string successMessage)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return ServiceResult.Fail("Account number is required.");

        var normalizedAccountNumber = accountNumber.Trim();
        var account = await unitOfWork.BankAccounts.GetByAccountNumberAsync(normalizedAccountNumber);
        if (account is null) return ServiceResult.Fail("Account not found.");

        if (account.Status == targetStatus) return ServiceResult.Fail(alreadyInStatusMessage);

        account.Status = targetStatus;
        await unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok(successMessage);
    }
}
