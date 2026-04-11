using BankAccountSimulatorMVC.Models;
using BankAccountSimulatorMVC.Services.Interface;

namespace BankAccountSimulatorMVC.Services
{
    public class TransactionService : ITransactionService
    {
        private const decimal MinimumBalanceAfterWithdraw = 100m;

        private readonly JsonBankDataStore _dataStore;
        private readonly decimal _dailyLimit;

        public TransactionService(JsonBankDataStore dataStore, IConfiguration configuration)
        {
            _dataStore = dataStore;
            _dailyLimit = configuration.GetValue<decimal>("Config:DailyLimit", decimal.MaxValue);
        }

        public async Task<ServiceResult> DepositAsync(string accountNumber, decimal amount)
        {
            if (amount <= 0) return ServiceResult.Fail("Deposit amount must be greater than 0.");

            var accounts = await _dataStore.ReadAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));
            if (account is null) return ServiceResult.Fail("Account not found.");
            if (account.Status == AccountStatus.Frozen) return ServiceResult.Fail("Frozen account operation is not allowed.");

            account.Balance += amount;

            var transactions = await _dataStore.ReadTransactionsAsync();
            transactions.Add(new Transaction
            {
                Id = JsonBankDataStore.GetNextTransactionId(transactions),
                AccountNumber = account.AccountNumber,
                Type = TransactionType.Deposit,
                Amount = amount,
                CreatedAt = DateTime.Now,
                Description = "Deposit successful"
            });

            await _dataStore.WriteAccountsAsync(accounts);
            await _dataStore.WriteTransactionsAsync(transactions);

            return ServiceResult.Ok("Deposit completed.");
        }

        public async Task<ServiceResult> WithdrawAsync(string accountNumber, decimal amount)
        {
            if (amount <= 0) return ServiceResult.Fail("Withdrawal amount must be greater than 0.");

            var accounts = await _dataStore.ReadAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));
            if (account is null) return ServiceResult.Fail("Account not found.");
            if (account.Status == AccountStatus.Frozen) return ServiceResult.Fail("Frozen account operation is not allowed.");
            if (account.Balance < amount) return ServiceResult.Fail("Insufficient balance.");
            if (account.Balance - amount < MinimumBalanceAfterWithdraw) return ServiceResult.Fail("Minimum balance must remain at least 100 after withdrawal.");

            var transactions = await _dataStore.ReadTransactionsAsync();
            var totalTodayOut = GetTodayOutgoingTotal(transactions, account.AccountNumber);
            if (totalTodayOut + amount > _dailyLimit) return ServiceResult.Fail($"Daily transaction limit exceeded ({_dailyLimit:N2}).");

            account.Balance -= amount;

            transactions.Add(new Transaction
            {
                Id = JsonBankDataStore.GetNextTransactionId(transactions),
                AccountNumber = account.AccountNumber,
                Type = TransactionType.Withdraw,
                Amount = amount,
                CreatedAt = DateTime.Now,
                Description = "Withdrawal successful"
            });

            await _dataStore.WriteAccountsAsync(accounts);
            await _dataStore.WriteTransactionsAsync(transactions);

            return ServiceResult.Ok("Withdrawal completed.");
        }

        public async Task<ServiceResult> TransferAsync(string sourceAccountNumber, string destinationAccountNumber, decimal amount)
        {
            if (string.Equals(sourceAccountNumber, destinationAccountNumber, StringComparison.OrdinalIgnoreCase)) return ServiceResult.Fail("Source and destination account must be different.");
            if (amount <= 0) return ServiceResult.Fail("Transfer amount must be greater than 0.");

            var accounts = await _dataStore.ReadAccountsAsync();
            var source = accounts.FirstOrDefault(a => a.AccountNumber.Equals(sourceAccountNumber, StringComparison.OrdinalIgnoreCase));
            var destination = accounts.FirstOrDefault(a => a.AccountNumber.Equals(destinationAccountNumber, StringComparison.OrdinalIgnoreCase));

            if (source is null || destination is null) return ServiceResult.Fail("Source or destination account does not exist.");
            if (source.Status == AccountStatus.Frozen || destination.Status == AccountStatus.Frozen) return ServiceResult.Fail("Frozen account operation is not allowed.");
            if (source.Balance < amount) return ServiceResult.Fail("Insufficient balance.");

            var transactions = await _dataStore.ReadTransactionsAsync();
            var totalTodayOut = GetTodayOutgoingTotal(transactions, source.AccountNumber);
            if (totalTodayOut + amount > _dailyLimit) return ServiceResult.Fail($"Daily transaction limit exceeded ({_dailyLimit:N2}).");

            var originalSourceBalance = source.Balance;
            var originalDestinationBalance = destination.Balance;

            var originalTransactions = transactions.ToList();
            var transferOutId = JsonBankDataStore.GetNextTransactionId(transactions);

            source.Balance -= amount;
            destination.Balance += amount;

            transactions.Add(new Transaction
            {
                Id = transferOutId,
                AccountNumber = source.AccountNumber,
                Type = TransactionType.Transfer,
                Amount = amount,
                CreatedAt = DateTime.Now,
                Description = $"Transfer out to {destination.AccountNumber}"
            });

            transactions.Add(new Transaction
            {
                Id = transferOutId + 1,
                AccountNumber = destination.AccountNumber,
                Type = TransactionType.Transfer,
                Amount = amount,
                CreatedAt = DateTime.Now,
                Description = $"Transfer in from {source.AccountNumber}"
            });

            try
            {
                await _dataStore.WriteAccountsAsync(accounts);
                await _dataStore.WriteTransactionsAsync(transactions);
            }
            catch
            {
                source.Balance = originalSourceBalance;
                destination.Balance = originalDestinationBalance;

                try
                {
                    await _dataStore.WriteAccountsAsync(accounts);
                    await _dataStore.WriteTransactionsAsync(originalTransactions);
                }
                catch
                {
                    return ServiceResult.Fail("Transfer failed. Rollback was attempted but did not complete.");
                }

                return ServiceResult.Fail("Transfer failed and was rolled back.");
            }

            return ServiceResult.Ok("Transfer completed.");
        }

        public async Task<IReadOnlyList<Transaction>> GetHistoryAsync(string? accountNumber, TransactionType? filterType)
        {
            var transactions = await _dataStore.ReadTransactionsAsync();
            IEnumerable<Transaction> query = transactions;

            if (!string.IsNullOrWhiteSpace(accountNumber)) query = query.Where(t => t.AccountNumber.Equals(accountNumber.Trim(), StringComparison.OrdinalIgnoreCase));
            if (filterType.HasValue) query = query.Where(t => t.Type == filterType.Value);

            return query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .ToList();
        }

        private decimal GetTodayOutgoingTotal(IEnumerable<Transaction> transactions, string accountNumber)
        {
            var today = DateTime.Today;

            return transactions
                .Where(t => t.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase))
                .Where(t => t.CreatedAt.Date == today)
                .Where(t => t.Type == TransactionType.Withdraw || (t.Type == TransactionType.Transfer && !IsTransferIn(t.Description)))
                .Sum(t => t.Amount);
        }

        private static bool IsTransferIn(string description)
        {
            return description.StartsWith("Transfer in", StringComparison.OrdinalIgnoreCase);
        }
    }
}
