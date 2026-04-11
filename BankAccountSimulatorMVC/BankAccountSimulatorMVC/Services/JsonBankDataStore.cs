using System.Text.Json;
using BankAccountSimulatorMVC.Models;

namespace BankAccountSimulatorMVC.Services
{
    public class JsonBankDataStore
    {
        private readonly string _accountsFilePath;
        private readonly string _transactionsFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public JsonBankDataStore(IWebHostEnvironment environment)
        {
            var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);

            _accountsFilePath = Path.Combine(dataDirectory, "BankAccounts.json");
            _transactionsFilePath = Path.Combine(dataDirectory, "Transaction.json");

            EnsureJsonFileExists(_accountsFilePath);
            EnsureJsonFileExists(_transactionsFilePath);
        }

        public async Task<List<BankAccount>> ReadAccountsAsync()
        {
            try
            {
                var content = await File.ReadAllTextAsync(_accountsFilePath);
                return JsonSerializer.Deserialize<List<BankAccount>>(content, _jsonOptions) ?? new List<BankAccount>();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                throw new DataStoreException("Could not read account data from JSON file.", ex);
            }
        }

        public async Task WriteAccountsAsync(List<BankAccount> accounts)
        {
            try
            {
                var content = JsonSerializer.Serialize(accounts, _jsonOptions);
                await File.WriteAllTextAsync(_accountsFilePath, content);
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                throw new DataStoreException("Could not write account data to JSON file.", ex);
            }
        }

        public async Task<List<Transaction>> ReadTransactionsAsync()
        {
            try
            {
                var content = await File.ReadAllTextAsync(_transactionsFilePath);
                if (string.IsNullOrWhiteSpace(content)) return new List<Transaction>();

                using var document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind != JsonValueKind.Array) return new List<Transaction>();

                var transactions = new List<Transaction>();
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    var typeValue = ReadInt(item, "Type") ?? 0;
                    var transaction = new Transaction
                    {
                        Id = ReadInt(item, "Id") ?? ReadInt(item, "TransactionId") ?? 0,
                        AccountNumber = ReadString(item, "AccountNumber") ?? string.Empty,
                        Type = Enum.IsDefined(typeof(TransactionType), typeValue)
                            ? (TransactionType)typeValue
                            : TransactionType.Transfer,
                        Amount = ReadDecimal(item, "Amount") ?? 0,
                        CreatedAt = ReadDateTime(item, "CreatedAt")
                            ?? ReadDateTime(item, "Date")
                            ?? DateTime.Now,
                        Description = ReadString(item, "Description") ?? string.Empty
                    };

                    transactions.Add(transaction);
                }

                foreach (var transaction in transactions)
                {
                    if (!Enum.IsDefined(typeof(TransactionType), transaction.Type)) transaction.Type = TransactionType.Transfer;
                }

                return transactions;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                throw new DataStoreException("Could not read transaction data from JSON file.", ex);
            }
        }

        public async Task WriteTransactionsAsync(List<Transaction> transactions)
        {
            try
            {
                var content = JsonSerializer.Serialize(transactions, _jsonOptions);
                await File.WriteAllTextAsync(_transactionsFilePath, content);
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                throw new DataStoreException("Could not write transaction data to JSON file.", ex);
            }
        }

        public static int GetNextTransactionId(List<Transaction> transactions)
        {
            if (transactions.Count == 0) return 1;

            return transactions.Max(t => t.Id) + 1;
        }

        private static string? ReadString(JsonElement item, string propertyName)
        {
            var value = GetPropertyValue(item, propertyName);
            if (value is null || value.Value.ValueKind == JsonValueKind.Null) return null;

            return value.Value.ValueKind == JsonValueKind.String ? value.Value.GetString() : value.Value.ToString();
        }

        private static int? ReadInt(JsonElement item, string propertyName)
        {
            var value = GetPropertyValue(item, propertyName);
            if (value is null) return null;

            if (value.Value.ValueKind == JsonValueKind.Number)
            {
                try
                {
                    return value.Value.GetInt32();
                }
                catch
                {
                    return null;
                }
            }

            if (value.Value.ValueKind == JsonValueKind.String)
            {
                try
                {
                    return int.Parse(value.Value.GetString() ?? string.Empty);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static decimal? ReadDecimal(JsonElement item, string propertyName)
        {
            var value = GetPropertyValue(item, propertyName);
            if (value is null) return null;

            if (value.Value.ValueKind == JsonValueKind.Number)
            {
                try
                {
                    return value.Value.GetDecimal();
                }
                catch
                {
                    return null;
                }
            }

            if (value.Value.ValueKind == JsonValueKind.String)
            {
                try
                {
                    return decimal.Parse(value.Value.GetString() ?? string.Empty);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static DateTime? ReadDateTime(JsonElement item, string propertyName)
        {
            var value = GetPropertyValue(item, propertyName);
            if (value is null) return null;

            if (value.Value.ValueKind == JsonValueKind.String)
            {
                try
                {
                    return DateTime.Parse(value.Value.GetString() ?? string.Empty);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static JsonElement? GetPropertyValue(JsonElement item, string propertyName)
        {
            foreach (var property in item.EnumerateObject())
            {
                if (property.NameEquals(propertyName)) return property.Value;
            }

            return null;
        }

        private static void EnsureJsonFileExists(string filePath)
        {
            if (!File.Exists(filePath)) File.WriteAllText(filePath, "[]");
        }
    }
}
