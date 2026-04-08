using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace BankAccountSimulator
{
    public class TransactionManage
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        private List<Transaction> Transactions = new List<Transaction>();
        public static readonly TransactionManage Instance = new TransactionManage();

        private TransactionManage()
        {
            _filePath = ResolveTransactionsFilePath();
            LoadTransactions();
        }

        public void RecordDeposit(string accountNumber, decimal amount)
        {
            AddTransaction(accountNumber, TransactionType.Deposit, amount, "Deposit successful");
        }

        public void RecordWithdrawal(string accountNumber, decimal amount)
        {
            AddTransaction(accountNumber, TransactionType.Withdrawal, amount, "Withdrawal successful");
        }

        public void RecordTransfer(string sourceAccountNumber, string destinationAccountNumber, decimal amount)
        {
            AddTransaction(sourceAccountNumber, TransactionType.TransferOut, amount, $"Transfer Out to {destinationAccountNumber}");
            AddTransaction(destinationAccountNumber, TransactionType.TransferIn, amount, $"Transfer In from {sourceAccountNumber}");
        }

        public List<Transaction> GetTransactionsByAccount(string accountNumber, int filterOption)
        {
            var query = Transactions.Where(t => t.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));

            if (filterOption == 2)
            {
                query = query.Where(t => t.Type == TransactionType.Deposit);
            }
            else if (filterOption == 3)
            {
                query = query.Where(t => t.Type == TransactionType.Withdrawal);
            }

            return query.OrderByDescending(t => t.Date).ToList();
        }

        public decimal GetTodayWithdrawalTotal(string accountNumber)
        {
            var today = DateTime.Now.Date;
            return Transactions
                .Where(t => t.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase)
                            && t.Type == TransactionType.Withdrawal
                            && t.Date.Date == today)
                .Sum(t => t.Amount);
        }


        #region private methods
        private void AddTransaction(string accountNumber, TransactionType type, decimal amount, string description)
        {
            var nextId = Transactions.Count == 0 ? 1 : Transactions.Max(t => t.TransactionId) + 1;
            var transaction = new Transaction
            {
                TransactionId = nextId,
                AccountNumber = accountNumber,
                Type = type,
                Amount = amount,
                Date = DateTime.Now,
                Description = description,
            };

            Transactions.Add(transaction);
            SaveTransactionsToFile();
        }

        private void LoadTransactions()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    SaveTransactionsToFile();
                    return;
                }

                var json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Transactions = new List<Transaction>();
                    return;
                }

                Transactions = JsonSerializer.Deserialize<List<Transaction>>(json, _jsonOptions) ?? new List<Transaction>();
            }
            catch (IOException ex)
            {
                Transactions = new List<Transaction>();
                Console.WriteLine($"I/O error while loading Transactions: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Transactions = new List<Transaction>();
                Console.WriteLine($"JSON parsing error: {ex.Message}");
            }
        }

        private void SaveTransactionsToFile()
        {
            try
            {
                var json = JsonSerializer.Serialize(Transactions, _jsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O error while saving Transactions: {ex.Message}");
            }
        }

        private string ResolveTransactionsFilePath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                var csproj = directory.GetFiles("*.csproj").FirstOrDefault();
                if (csproj != null)
                {
                    return Path.Combine(directory.FullName, "Transaction.json");
                }

                directory = directory.Parent;
            }

            return Path.Combine(Directory.GetCurrentDirectory(), "Transaction.json");
        }

        #endregion
    }
}
