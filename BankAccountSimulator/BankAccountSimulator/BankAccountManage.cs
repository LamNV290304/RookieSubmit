using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace BankAccountSimulator
{
    public class BankAccountManage
    {
        private readonly string _filePath;
        private readonly string _configFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        private readonly decimal _minimumBalanceAfterWithdrawal;
        private readonly decimal _dailyWithdrawalLimit;
        private readonly decimal _monthlyInterestRate;
        private List<BankAccount> BankAccounts = new List<BankAccount>();
        public static readonly BankAccountManage Instance = new BankAccountManage();
        private BankAccountManage()
        {
            _filePath = ResolveBankAccountsFilePath();
            _configFilePath = ResolveConfigFilePath();
            var config = LoadConfig();
            _minimumBalanceAfterWithdrawal = config.MinimumBalanceAfterWithdrawal;
            _dailyWithdrawalLimit = config.DailyWithdrawalLimit;
            _monthlyInterestRate = config.MonthlyInterestRate;
            LoadBankAccounts();
        }

        public void CreateAccount()
        {
            try
            {
                var accountNumber = InputAccountNumber();
                if (accountNumber == null) return;

                var ownerName = InputOwnerName();
                if (ownerName == null) return;

                var initialBalance = InputInitialBalance();
                if (initialBalance == null) return;

                var newAccount = new BankAccount
                {
                    AccountNumber = accountNumber,
                    OwnerName = ownerName,
                    Balance = initialBalance.Value,
                };

                BankAccounts.Add(newAccount);
                SaveBankAccountsToFile();

                Console.WriteLine("Account created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create account failed: {ex.Message}");
            }
        }

        public void Deposit()
        {
            try
            {
                var account = InputActiveAccount();
                if (account == null) return;

                var amount = InputDepositAmount();
                if (amount == null) return;

                account.Balance += amount.Value;
                SaveBankAccountsToFile();

                TransactionManage.Instance.RecordDeposit(account.AccountNumber, amount.Value);

                Console.WriteLine($"Updated balance: {account.Balance:N2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deposit failed: {ex.Message}");
            }
        }

        public void Withdraw()
        {
            try
            {
                var account = InputActiveAccount();
                if (account == null) return;

                var amount = InputWithdrawAmount(account);
                if (amount == null) return;

                account.Balance -= amount.Value;
                SaveBankAccountsToFile();

                TransactionManage.Instance.RecordWithdrawal(account.AccountNumber, amount.Value);

                Console.WriteLine("Withdraw successful.");
                Console.WriteLine($"Remaining balance: {account.Balance:N2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Withdraw failed: {ex.Message}");
            }
        }

        public void Transfer()
        {
            try
            {
                var sourceAccount = InputActiveAccount("Enter source account number: ");
                if (sourceAccount == null) return;

                var destinationAccount = InputExistingAccount("Enter destination account number: ");
                if (destinationAccount == null) return;

                if (sourceAccount.AccountNumber.Equals(destinationAccount.AccountNumber, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Invalid transfer. Source and destination accounts must be different.");
                    return;
                }

                if (destinationAccount.Status != Status.Active)
                {
                    Console.WriteLine("Invalid transfer. Destination account must be Active.");
                    return;
                }

                var amount = InputTransferAmount(sourceAccount);
                if (amount == null) return;

                sourceAccount.Balance -= amount.Value;
                destinationAccount.Balance += amount.Value;
                SaveBankAccountsToFile();

                TransactionManage.Instance.RecordTransfer(sourceAccount.AccountNumber, destinationAccount.AccountNumber, amount.Value);

                Console.WriteLine("Transfer completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Transfer failed: {ex.Message}");
            }
        }

        public void ViewAccountDetails()
        {
            try
            {
                var account = InputExistingAccount();
                if (account == null) return;

                Console.WriteLine("=== Account Details ===");
                Console.WriteLine(account.ToString());
                Console.WriteLine("View account details successful.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"View account details failed: {ex.Message}");
            }
        }

        public void ViewTransactionHistory()
        {
            try
            {
                var account = InputExistingAccount();
                if (account == null) return;

                var filterOption = InputTransactionFilterOption();
                if (filterOption == null) return;

                var transactions = TransactionManage.Instance.GetTransactionsByAccount(account.AccountNumber, filterOption.Value);
                if (transactions.Count == 0)
                {
                    Console.WriteLine("No transactions found.");
                    return;
                }

                Console.WriteLine("=== Transaction History ===");
                foreach (var transaction in transactions)
                {
                    Console.WriteLine(transaction.ToString());
                }

                Console.WriteLine("View transaction history successful.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"View transaction history failed: {ex.Message}");
            }
        }

        public void ChangeAccountStatus()
        {
            try
            {
                var account = InputExistingAccount();
                if (account == null) return;

                var freezeOption = InputFreezeOption();
                if (freezeOption == null) return;

                account.Status = freezeOption.Value == 1 ? Status.Frozen : Status.Active;
                SaveBankAccountsToFile();

                Console.WriteLine("Account status updated successfully.");
                Console.WriteLine($"Current status: {account.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Change account status failed: {ex.Message}");
            }
        }

        public void CalculateMonthlyInterest()
        {
            try
            {
                var account = InputExistingAccount();
                if (account == null) return;

                var months = InputMonthCount();
                if (months == null) return;

                var currentBalance = account.Balance;
                var futureBalance = currentBalance * (decimal)Math.Pow((double)(1 + _monthlyInterestRate), months.Value);
                var interestIncrease = futureBalance - currentBalance;

                Console.WriteLine("Interest calculation successful.");
                Console.WriteLine($"Monthly interest rate: {_monthlyInterestRate:P2}");
                Console.WriteLine($"Months: {months.Value}");
                Console.WriteLine($"Interest increase: {interestIncrease:N2}");
                Console.WriteLine($"Projected balance: {futureBalance:N2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Interest calculation failed: {ex.Message}");
            }
        }


        #region private methods
        private string? InputAccountNumber()
        {
            while (true)
            {
                Console.Write("Enter account number: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Account number cannot be empty. Please try again.");
                    continue;
                }

                var accountNumber = input.Trim();
                try
                {
                    ValidateAccountNumber(accountNumber);
                    return accountNumber;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private string? InputOwnerName()
        {
            while (true)
            {
                Console.Write("Enter owner name: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Owner name cannot be empty. Please try again.");
                    continue;
                }

                var ownerName = input.Trim();
                try
                {
                    ValidateOwnerName(ownerName);
                    return ownerName;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private decimal? InputInitialBalance()
        {
            while (true)
            {
                Console.Write("Initial balance: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Initial balance cannot be empty. Please try again.");
                    continue;
                }

                try
                {
                    return ValidateInitialBalance(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private BankAccount? InputActiveAccount()
        {
            return InputActiveAccount("Enter account number: ");
        }

        private BankAccount? InputActiveAccount(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Account number cannot be empty. Please try again.");
                    continue;
                }

                var accountNumber = input.Trim();
                var account = FindAccountByNumber(accountNumber);
                if (account == null)
                {
                    Console.WriteLine("Account does not exist.");
                    continue;
                }

                if (account.Status != Status.Active)
                {
                    Console.WriteLine("Account status must be Active.");
                    continue;
                }

                return account;
            }
        }

        private BankAccount? InputExistingAccount()
        {
            return InputExistingAccount("Enter account number: ");
        }

        private BankAccount? InputExistingAccount(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Account number cannot be empty. Please try again.");
                    continue;
                }

                var accountNumber = input.Trim();
                var account = FindAccountByNumber(accountNumber);
                if (account == null)
                {
                    Console.WriteLine("Account does not exist.");
                    continue;
                }

                return account;
            }
        }

        private decimal? InputDepositAmount()
        {
            while (true)
            {
                Console.Write("Enter amount: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Amount cannot be empty. Please try again.");
                    continue;
                }

                try
                {
                    return ValidateDepositAmount(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private decimal? InputWithdrawAmount(BankAccount account)
        {
            while (true)
            {
                Console.Write("Enter amount: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Amount cannot be empty. Please try again.");
                    continue;
                }

                try
                {
                    var amount = ValidateDepositAmount(input);

                    if (amount > account.Balance)
                    {
                        throw new InvalidOperationException("Amount must not exceed balance.");
                    }

                    var balanceAfterWithdrawal = account.Balance - amount;
                    if (balanceAfterWithdrawal < _minimumBalanceAfterWithdrawal)
                    {
                        throw new InvalidOperationException($"Balance after withdrawal must be at least {_minimumBalanceAfterWithdrawal:N0}.");
                    }

                    var todayWithdrawalTotal = TransactionManage.Instance.GetTodayWithdrawalTotal(account.AccountNumber);
                    if (todayWithdrawalTotal + amount > _dailyWithdrawalLimit)
                    {
                        throw new InvalidOperationException($"Daily withdrawal limit exceeded. Limit: {_dailyWithdrawalLimit:N2}, withdrawn today: {todayWithdrawalTotal:N2}.");
                    }

                    return amount;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private decimal? InputTransferAmount(BankAccount sourceAccount)
        {
            while (true)
            {
                Console.Write("Enter transfer amount: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Transfer amount cannot be empty. Please try again.");
                    continue;
                }

                try
                {
                    var amount = ValidateDepositAmount(input);
                    if (amount > sourceAccount.Balance)
                    {
                        throw new InvalidOperationException("Source account must have sufficient funds.");
                    }

                    return amount;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private int? InputTransactionFilterOption()
        {
            while (true)
            {
                Console.WriteLine("Choose filter: 1. All 2. Deposits only 3. Withdrawals only");
                Console.Write("Filter option: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Filter option cannot be empty. Please try again.");
                    continue;
                }

                if (input == "1" || input == "2" || input == "3")
                {
                    return int.Parse(input);
                }

                Console.WriteLine("Invalid filter option.");
            }
        }

        private int? InputFreezeOption()
        {
            while (true)
            {
                Console.WriteLine("Choose status action: 1. Freeze 2. Unfreeze");
                Console.Write("Status option: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Status option cannot be empty. Please try again.");
                    continue;
                }

                if (input == "1" || input == "2")
                {
                    return int.Parse(input);
                }

                Console.WriteLine("Invalid option.");
            }
        }

        private int? InputMonthCount()
        {
            while (true)
            {
                Console.Write("Enter month count: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Month count cannot be empty. Please try again.");
                    continue;
                }

                if (int.TryParse(input, out var months) && months > 0)
                {
                    return months;
                }

                Console.WriteLine("Month count must be a positive integer.");
            }
        }

        private void ValidateAccountNumber(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber)) throw new ArgumentException("Account number cannot be empty.");
            if (BankAccounts.Any(a => a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Account number must be unique.");
        }

        private void ValidateOwnerName(string ownerName)
        {
            if (string.IsNullOrWhiteSpace(ownerName)) throw new ArgumentException("Owner name cannot be empty.");
        }

        private decimal ValidateInitialBalance(string initialBalanceInput)
        {
            if (!decimal.TryParse(initialBalanceInput, out var initialBalance))  throw new ArgumentException("Initial balance is invalid.");
            if (initialBalance < 0) throw new ArgumentException("Initial balance must be greater than or equal to 0.");

            return initialBalance;
        }

        private decimal ValidateDepositAmount(string amountInput)
        {
            if (!decimal.TryParse(amountInput, out var amount)) throw new ArgumentException("Amount is invalid.");
            if (amount <= 0) throw new ArgumentException("Amount must be greater than 0.");

            return amount;
        }

        private BankAccount? FindAccountByNumber(string accountNumber)
        {
            return BankAccounts.FirstOrDefault(a => a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadBankAccounts()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    SaveBankAccountsToFile();
                    return;
                }

                var json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    BankAccounts = new List<BankAccount>();
                    return;
                }

                BankAccounts = JsonSerializer.Deserialize<List<BankAccount>>(json, _jsonOptions) ?? new List<BankAccount>();
            }
            catch (IOException ex)
            {
                BankAccounts = new List<BankAccount>();
                Console.WriteLine($"I/O error while loading BankAccounts: {ex.Message}");
            }
            catch (JsonException ex)
            {
                BankAccounts = new List<BankAccount>();
                Console.WriteLine($"JSON parsing error: {ex.Message}");
            }
        }

        private void SaveBankAccountsToFile()
        {
            try
            {
                var json = JsonSerializer.Serialize(BankAccounts, _jsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O error while saving BankAccounts: {ex.Message}");
            }
        }

        private AppConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    return SaveDefaultConfig();
                }

                var json = File.ReadAllText(_configFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return SaveDefaultConfig();
                }

                var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
                if (config == null || config.MinimumBalanceAfterWithdrawal < 0 || config.DailyWithdrawalLimit < 0 || config.MonthlyInterestRate < 0)
                {
                    return SaveDefaultConfig();
                }

                return config;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O error while loading config: {ex.Message}");
                return SaveDefaultConfig();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error while loading config: {ex.Message}");
                return SaveDefaultConfig();
            }
        }

        private AppConfig SaveDefaultConfig()
        {
            var defaultMinValue = 100m;
            var defaultDailyLimit = 2000m;
            var defaultMonthlyInterestRate = 0.01m;
            try
            {
                var config = new AppConfig
                {
                    MinimumBalanceAfterWithdrawal = defaultMinValue,
                    DailyWithdrawalLimit = defaultDailyLimit,
                    MonthlyInterestRate = defaultMonthlyInterestRate,
                };

                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(_configFilePath, json);
                return config;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O error while saving default config: {ex.Message}");
                return new AppConfig
                {
                    MinimumBalanceAfterWithdrawal = defaultMinValue,
                    DailyWithdrawalLimit = defaultDailyLimit,
                    MonthlyInterestRate = defaultMonthlyInterestRate,
                };
            }
        }

        private string ResolveBankAccountsFilePath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                var csproj = directory.GetFiles("*.csproj").FirstOrDefault();
                if (csproj != null)
                {
                    return Path.Combine(directory.FullName, "BankAccounts.json");
                }

                directory = directory.Parent;
            }

            return Path.Combine(Directory.GetCurrentDirectory(), "BankAccounts.json");
        }

        private string ResolveConfigFilePath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                var csproj = directory.GetFiles("*.csproj").FirstOrDefault();
                if (csproj != null)
                {
                    return Path.Combine(directory.FullName, "config.json");
                }

                directory = directory.Parent;
            }

            return Path.Combine(Directory.GetCurrentDirectory(), "config.json");
        }

        private class AppConfig
        {
            public decimal MinimumBalanceAfterWithdrawal { get; set; }
            public decimal DailyWithdrawalLimit { get; set; }
            public decimal MonthlyInterestRate { get; set; }
        }

        #endregion
    }
}
