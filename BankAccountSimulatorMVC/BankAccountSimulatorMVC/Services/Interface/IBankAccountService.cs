using Domain.Models;
using BankAccountSimulatorMVC.Services;

namespace BankAccountSimulatorMVC.Services.Interface;

public interface IBankAccountService
{
    Task<IReadOnlyList<BankAccount>> GetAllAsync();
    Task<BankAccount?> GetByAccountNumberAsync(string accountNumber);
    Task<ServiceResult> CreateAsync(BankAccount account);
    Task<ServiceResult> FreezeAsync(string accountNumber);
    Task<ServiceResult> UnfreezeAsync(string accountNumber);
}
