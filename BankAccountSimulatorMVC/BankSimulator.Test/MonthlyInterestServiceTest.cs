using Moq;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Storage;
using BankAccountSimulatorMVC.Interfaces;
using BankAccountSimulatorMVC.Services;

namespace BankSimulator.Test
{
    public class MonthlyInterestServiceTest
    {
        private readonly Mock<IBankAccountRepository> _mockAccountRepo;
        private readonly Mock<ITransactionRepository> _mockTransactionRepo;
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<MonthlyInterestService>> _mockLogger;

        public MonthlyInterestServiceTest()
        {
            _mockAccountRepo = new Mock<IBankAccountRepository>();
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<MonthlyInterestService>>();

            _mockUow.Setup(x => x.BankAccounts).Returns(_mockAccountRepo.Object);
            _mockUow.Setup(x => x.Transactions).Returns(_mockTransactionRepo.Object);
        }

        private MonthlyInterestService CreateService(decimal monthlyRate)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Config:MonthlyInterest"] = monthlyRate.ToString(System.Globalization.CultureInfo.InvariantCulture)
                })
                .Build();

            return new MonthlyInterestService(_mockUow.Object, configuration, _mockLogger.Object);
        }

        [Fact]
        public async Task ApplyCurrentMonthAsync_ShouldFail_WhenMonthlyRateIsNotGreaterThanZero()
        {
            // Arrange
            var service = CreateService(0m);

            // Act
            var result = await service.ApplyCurrentMonthAsync();

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Monthly interest rate must be greater than 0.", result.Message);
        }

        [Fact]
        public async Task ApplyCurrentMonthAsync_ShouldReturnOk_WhenNoActiveAccounts()
        {
            // Arrange
            var service = CreateService(0.02m);
            _mockAccountRepo.Setup(x => x.GetActiveForUpdateAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync([]);

            // Act
            var result = await service.ApplyCurrentMonthAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal("No active accounts to apply monthly interest.", result.Message);
        }

        [Fact]
        public async Task ApplyCurrentMonthAsync_ShouldSkipSave_WhenAllAccountsAlreadyApplied()
        {
            // Arrange
            var service = CreateService(0.02m);
            var dbTransaction = new Mock<IDbContextTransaction>();
            var accounts = new List<BankAccount>
            {
                new() { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active },
                new() { AccountNumber = "A002", Balance = 2000, Status = AccountStatus.Active }
            };

            _mockAccountRepo.Setup(x => x.GetActiveForUpdateAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(accounts);
            _mockTransactionRepo.Setup(x => x.HasMonthlyInterestAppliedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(true);
            _mockUow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dbTransaction.Object);

            // Act
            var result = await service.ApplyCurrentMonthAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Monthly interest applied for 0 account(s).", result.Message);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            dbTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApplyCurrentMonthAsync_ShouldSkipAccount_WhenCalculatedInterestIsNotGreaterThanZero()
        {
            // Arrange
            var service = CreateService(0.02m);
            var dbTransaction = new Mock<IDbContextTransaction>();
            var account = new BankAccount { AccountNumber = "A001", Balance = 0, Status = AccountStatus.Active };

            _mockAccountRepo.Setup(x => x.GetActiveForUpdateAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync([account]);
            _mockTransactionRepo.Setup(x => x.HasMonthlyInterestAppliedAsync("A001", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(false);
            _mockUow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dbTransaction.Object);

            // Act
            var result = await service.ApplyCurrentMonthAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Monthly interest applied for 0 account(s).", result.Message);
            Assert.Equal(0, account.Balance);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            dbTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApplyCurrentMonthAsync_ShouldApplyInterestAndSave_WhenEligibleAccountsExist()
        {
            // Arrange
            var service = CreateService(0.02m);
            var dbTransaction = new Mock<IDbContextTransaction>();
            var account = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };

            _mockAccountRepo.Setup(x => x.GetActiveForUpdateAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync([account]);
            _mockTransactionRepo.Setup(x => x.HasMonthlyInterestAppliedAsync("A001", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(false);
            _mockUow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dbTransaction.Object);

            // Act
            var result = await service.ApplyCurrentMonthAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Monthly interest applied for 1 account(s).", result.Message);
            Assert.Equal(1020, account.Balance);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.Is<Transaction>(t =>
                t.AccountNumber == "A001" &&
                t.Type == TransactionType.Deposit &&
                t.Amount == 20 &&
                t.Description.Contains("Monthly interest")), It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            dbTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApplyCurrentMonthAsync_ShouldApplyOnlyForNotYetAppliedAccounts()
        {
            // Arrange
            var service = CreateService(0.02m);
            var dbTransaction = new Mock<IDbContextTransaction>();
            var first = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };
            var second = new BankAccount { AccountNumber = "A002", Balance = 2000, Status = AccountStatus.Active };

            _mockAccountRepo.Setup(x => x.GetActiveForUpdateAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync([first, second]);
            _mockTransactionRepo.Setup(x => x.HasMonthlyInterestAppliedAsync("A001", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(true);
            _mockTransactionRepo.Setup(x => x.HasMonthlyInterestAppliedAsync("A002", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(false);
            _mockUow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dbTransaction.Object);

            // Act
            var result = await service.ApplyCurrentMonthAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Monthly interest applied for 1 account(s).", result.Message);
            Assert.Equal(1000, first.Balance);
            Assert.Equal(2040, second.Balance);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.Is<Transaction>(t => t.AccountNumber == "A002" && t.Amount == 40), It.IsAny<CancellationToken>()), Times.Once);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.Is<Transaction>(t => t.AccountNumber == "A001"), It.IsAny<CancellationToken>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            dbTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApplyCurrentMonthAsync_ShouldRollbackAndThrow_WhenExceptionOccurs()
        {
            // Arrange
            var service = CreateService(0.02m);
            var dbTransaction = new Mock<IDbContextTransaction>();
            var account = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };

            _mockAccountRepo.Setup(x => x.GetActiveForUpdateAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync([account]);
            _mockTransactionRepo.Setup(x => x.HasMonthlyInterestAppliedAsync("A001", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(false);
            _mockUow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dbTransaction.Object);
            _mockUow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("save error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyCurrentMonthAsync());
            dbTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            dbTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }

}
