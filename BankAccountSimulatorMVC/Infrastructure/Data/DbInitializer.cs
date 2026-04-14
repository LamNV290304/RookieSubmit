using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(BankDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        var hasAnyData = await dbContext.BankAccounts.AnyAsync(cancellationToken) || await dbContext.Transactions.AnyAsync(cancellationToken);

        if (hasAnyData)
        {
            return;
        }

        var accounts = new List<BankAccount>
        {
            new() { AccountNumber = "1001", OwnerName = "Nguyen Van An", Balance = 5000m, Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-15) },
            new() { AccountNumber = "1002", OwnerName = "Tran Thi Binh", Balance = 7200m, Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-14) },
            new() { AccountNumber = "1003", OwnerName = "Le Quoc Cuong", Balance = 9100m, Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-13) },
            new() { AccountNumber = "1004", OwnerName = "Pham Thu Dung", Balance = 4600m, Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-12) },
            new() { AccountNumber = "1005", OwnerName = "Hoang Gia Huy", Balance = 8300m, Status = AccountStatus.Frozen, CreatedAt = DateTime.UtcNow.AddDays(-11) },
            new() { AccountNumber = "1006", OwnerName = "Vo Minh Khang", Balance = 12000m, Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new() { AccountNumber = "1007", OwnerName = "Bui Thanh Lam", Balance = 3900m, Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-9) },
            new() { AccountNumber = "1008", OwnerName = "Dang Nhu Mai", Balance = 6600m, Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-8) },
            new() { AccountNumber = "1009", OwnerName = "Do Tuan Nam", Balance = 5400m, Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new() { AccountNumber = "1010", OwnerName = "Phan Bao Chau", Balance = 9800m, Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-6) }
        };

        await dbContext.BankAccounts.AddRangeAsync(accounts, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var transactions = new List<Transaction>
        {
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },
            new() { AccountNumber = "1001", Type = TransactionType.Deposit, Amount = 1200m, Description = "Initial deposit top-up", CreatedAt = now.AddDays(-5).AddHours(-9) },
            new() { AccountNumber = "1001", Type = TransactionType.Withdraw, Amount = 300m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-4).AddHours(-6) },

            new() { AccountNumber = "1002", Type = TransactionType.Deposit, Amount = 900m, Description = "Salary partial deposit", CreatedAt = now.AddDays(-5).AddHours(-4) },
            new() { AccountNumber = "1002", Type = TransactionType.Transfer, Amount = 250m, Description = "Transfer out to 1003", CreatedAt = now.AddDays(-3).AddHours(-8) },

            new() { AccountNumber = "1003", Type = TransactionType.Transfer, Amount = 250m, Description = "Transfer in from 1002", CreatedAt = now.AddDays(-3).AddHours(-8).AddMinutes(2) },
            new() { AccountNumber = "1003", Type = TransactionType.Withdraw, Amount = 500m, Description = "Bill payment", CreatedAt = now.AddDays(-2).AddHours(-7) },

            new() { AccountNumber = "1004", Type = TransactionType.Deposit, Amount = 600m, Description = "Cash counter deposit", CreatedAt = now.AddDays(-5).AddHours(-2) },
            new() { AccountNumber = "1004", Type = TransactionType.Transfer, Amount = 150m, Description = "Transfer out to 1007", CreatedAt = now.AddDays(-1).AddHours(-5) },

            new() { AccountNumber = "1005", Type = TransactionType.Deposit, Amount = 400m, Description = "Savings deposit", CreatedAt = now.AddDays(-6).AddHours(-10) },
            new() { AccountNumber = "1005", Type = TransactionType.Withdraw, Amount = 100m, Description = "Service charge", CreatedAt = now.AddDays(-4).AddHours(-1) },

            new() { AccountNumber = "1006", Type = TransactionType.Deposit, Amount = 2000m, Description = "Business income", CreatedAt = now.AddDays(-5).AddHours(-11) },
            new() { AccountNumber = "1006", Type = TransactionType.Transfer, Amount = 700m, Description = "Transfer out to 1008", CreatedAt = now.AddDays(-2).AddHours(-9) },

            new() { AccountNumber = "1007", Type = TransactionType.Transfer, Amount = 150m, Description = "Transfer in from 1004", CreatedAt = now.AddDays(-1).AddHours(-5).AddMinutes(1) },
            new() { AccountNumber = "1007", Type = TransactionType.Withdraw, Amount = 200m, Description = "ATM cash withdrawal", CreatedAt = now.AddDays(-1).AddHours(-2) },

            new() { AccountNumber = "1008", Type = TransactionType.Transfer, Amount = 700m, Description = "Transfer in from 1006", CreatedAt = now.AddDays(-2).AddHours(-9).AddMinutes(1) },
            new() { AccountNumber = "1008", Type = TransactionType.Deposit, Amount = 350m, Description = "Cashback received", CreatedAt = now.AddDays(-1).AddHours(-6) },

            new() { AccountNumber = "1009", Type = TransactionType.Deposit, Amount = 500m, Description = "Family support", CreatedAt = now.AddDays(-4).AddHours(-3) },
            new() { AccountNumber = "1009", Type = TransactionType.Withdraw, Amount = 120m, Description = "Utility payment", CreatedAt = now.AddDays(-2).AddHours(-1) },

            new() { AccountNumber = "1010", Type = TransactionType.Deposit, Amount = 1100m, Description = "Freelance income", CreatedAt = now.AddDays(-3).AddHours(-10) },
            new() { AccountNumber = "1010", Type = TransactionType.Withdraw, Amount = 240m, Description = "Online shopping", CreatedAt = now.AddDays(-1).AddHours(-7) }
        };

        await dbContext.Transactions.AddRangeAsync(transactions, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}