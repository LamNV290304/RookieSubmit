using BankAccountSimulatorMVC.Models;
using BankAccountSimulatorMVC.Services.Interface;

namespace BankAccountSimulatorMVC.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly JsonBankDataStore _dataStore;

        public BankAccountService(JsonBankDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task<IReadOnlyList<BankAccount>> GetAllAsync()
        {
            var accounts = await _dataStore.ReadAccountsAsync();
            return accounts
                .OrderBy(a => a.AccountNumber)
                .ToList();
        }

        public async Task<BankAccount?> GetByAccountNumberAsync(string accountNumber)
        {
            var accounts = await _dataStore.ReadAccountsAsync();
            return accounts.FirstOrDefault(a => a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<ServiceResult> CreateAsync(BankAccount account)
        {
            var accountNumber = account.AccountNumber.Trim();
            var ownerName = account.OwnerName.Trim();

            if (string.IsNullOrWhiteSpace(accountNumber)) return ServiceResult.Fail("Account number is required.");
            if (string.IsNullOrWhiteSpace(ownerName)) return ServiceResult.Fail("Owner name is required.");
            if (account.Balance < 0) return ServiceResult.Fail("Initial balance must be 0 or greater.");

            var accounts = await _dataStore.ReadAccountsAsync();
            if (accounts.Any(a => a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase))) return ServiceResult.Fail("Account number already exists.");

            account.AccountNumber = accountNumber;
            account.OwnerName = ownerName;
            account.Status = AccountStatus.Active;
            account.CreatedAt = DateTime.Now;

            accounts.Add(account);
            await _dataStore.WriteAccountsAsync(accounts);

            return ServiceResult.Ok("Account created successfully.");
        }

        public async Task<ServiceResult> FreezeAsync(string accountNumber)
        {
            return await SetAccountStatusAsync(accountNumber, AccountStatus.Frozen, "Account frozen.");
        }

        public async Task<ServiceResult> UnfreezeAsync(string accountNumber)
        {
            return await SetAccountStatusAsync(accountNumber, AccountStatus.Active, "Account unfrozen.");
        }

        private async Task<ServiceResult> SetAccountStatusAsync(string accountNumber, AccountStatus status, string successMessage)
        {
            var accounts = await _dataStore.ReadAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));
            if (account is null) return ServiceResult.Fail("Account not found.");

            account.Status = status;
            await _dataStore.WriteAccountsAsync(accounts);

            return ServiceResult.Ok(successMessage);
        }
    }
}
