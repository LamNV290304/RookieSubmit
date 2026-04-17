using Moq;
using Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Storage;
using BankAccountSimulatorMVC.Interfaces;
using BankAccountSimulatorMVC.Services;

namespace BankSimulator.Test
{
    public class TransactionServiceTest
    {
        private readonly Mock<IBankAccountRepository> _mockAccountRepo;
        private readonly Mock<ITransactionRepository> _mockTransactionRepo;
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly TransactionService _service;

        public TransactionServiceTest()
        {
            _mockAccountRepo = new Mock<IBankAccountRepository>();
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _mockUow = new Mock<IUnitOfWork>();

            _mockUow.Setup(x => x.BankAccounts).Returns(_mockAccountRepo.Object);
            _mockUow.Setup(x => x.Transactions).Returns(_mockTransactionRepo.Object);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Config:DailyLimit"] = "500"
                })
                .Build();

            _service = new TransactionService(_mockUow.Object, configuration);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DepositAsync_ShouldFail_WhenAccountNumberIsInvalid(string? accountNumber)
        {
            // Arrange

            // Act
            var result = await _service.DepositAsync(accountNumber!, 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account number is required.", result.Message);
        }

        [Fact]
        public async Task DepositAsync_ShouldSuccess_WhenAccountIsActiveAndAmountValid()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(account);

            // Act
            var result = await _service.DepositAsync(" A001 ", 200);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Deposit completed.", result.Message);
            Assert.Equal(1200, account.Balance);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.Is<Transaction>(t =>
                t.AccountNumber == "A001" &&
                t.Type == TransactionType.Deposit &&
                t.Amount == 200), It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DepositAsync_ShouldFail_WhenAmountIsNotGreaterThanZero()
        {
            // Arrange

            // Act
            var result = await _service.DepositAsync("A001", 0);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Amount must be greater than 0.", result.Message);
        }

        [Fact]
        public async Task DepositAsync_ShouldFail_WhenAccountNotFound()
        {
            // Arrange
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync((BankAccount?)null);

            // Act
            var result = await _service.DepositAsync("A001", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account not found.", result.Message);
        }

        [Fact]
        public async Task DepositAsync_ShouldFail_WhenAccountIsFrozen()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Frozen };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(account);

            // Act
            var result = await _service.DepositAsync("A001", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account is frozen.", result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task WithdrawAsync_ShouldFail_WhenAccountNumberIsInvalid(string? accountNumber)
        {
            // Arrange

            // Act
            var result = await _service.WithdrawAsync(accountNumber!, 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account number is required.", result.Message);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldFail_WhenAmountIsNotGreaterThanZero()
        {
            // Arrange

            // Act
            var result = await _service.WithdrawAsync("A001", 0);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Amount must be greater than 0.", result.Message);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldFail_WhenAccountNotFound()
        {
            // Arrange
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync((BankAccount?)null);

            // Act
            var result = await _service.WithdrawAsync("A001", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account not found.", result.Message);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldFail_WhenAccountIsFrozen()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Frozen };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(account);

            // Act
            var result = await _service.WithdrawAsync("A001", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account is frozen.", result.Message);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldFail_WhenInsufficientFunds()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = "A001", Balance = 150, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(account);

            // Act
            var result = await _service.WithdrawAsync("A001", 60);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Insufficient funds. Minimum balance after withdrawal is 100.", result.Message);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldFail_WhenDailyLimitExceeded()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(account);
            _mockTransactionRepo.Setup(x => x.GetTodayOutgoingTotalAsync("A001", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(450);

            // Act
            var result = await _service.WithdrawAsync("A001", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Daily outgoing limit exceeded. Limit is 500.", result.Message);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldSuccess_WhenInputIsValid()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(account);
            _mockTransactionRepo.Setup(x => x.GetTodayOutgoingTotalAsync("A001", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(100);

            // Act
            var result = await _service.WithdrawAsync("A001", 200);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Withdrawal completed.", result.Message);
            Assert.Equal(800, account.Balance);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.Is<Transaction>(t =>
                t.AccountNumber == "A001" &&
                t.Type == TransactionType.Withdraw &&
                t.Amount == 200), It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TransferAsync_ShouldFail_WhenSourceAndDestinationAreSame()
        {
            // Arrange

            // Act
            var result = await _service.TransferAsync("A001", "a001", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Source and destination account must be different.", result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task TransferAsync_ShouldFail_WhenSourceAccountIsInvalid(string? source)
        {
            // Arrange

            // Act
            var result = await _service.TransferAsync(source!, "A002", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Source account is required.", result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task TransferAsync_ShouldFail_WhenDestinationAccountIsInvalid(string? destination)
        {
            // Arrange

            // Act
            var result = await _service.TransferAsync("A001", destination!, 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Destination account is required.", result.Message);
        }

        [Fact]
        public async Task TransferAsync_ShouldFail_WhenAmountIsNotGreaterThanZero()
        {
            // Arrange

            // Act
            var result = await _service.TransferAsync("A001", "A002", 0);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Amount must be greater than 0.", result.Message);
        }

        [Fact]
        public async Task TransferAsync_ShouldFail_WhenSourceAccountNotFound()
        {
            // Arrange
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync((BankAccount?)null);

            // Act
            var result = await _service.TransferAsync("A001", "A002", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Source account not found.", result.Message);
        }

        [Fact]
        public async Task TransferAsync_ShouldFail_WhenDestinationAccountNotFound()
        {
            // Arrange
            var sourceAccount = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(sourceAccount);
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A002", It.IsAny<CancellationToken>()))
                            .ReturnsAsync((BankAccount?)null);

            // Act
            var result = await _service.TransferAsync("A001", "A002", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Destination account not found.", result.Message);
        }

        [Fact]
        public async Task TransferAsync_ShouldFail_WhenSourceAccountIsFrozen()
        {
            // Arrange
            var sourceAccount = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Frozen };
            var destinationAccount = new BankAccount { AccountNumber = "A002", Balance = 300, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(sourceAccount);
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A002", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(destinationAccount);

            // Act
            var result = await _service.TransferAsync("A001", "A002", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Source account is frozen.", result.Message);
        }

        [Fact]
        public async Task TransferAsync_ShouldFail_WhenDestinationAccountIsFrozen()
        {
            // Arrange
            var sourceAccount = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };
            var destinationAccount = new BankAccount { AccountNumber = "A002", Balance = 300, Status = AccountStatus.Frozen };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(sourceAccount);
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A002", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(destinationAccount);

            // Act
            var result = await _service.TransferAsync("A001", "A002", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Destination account is frozen.", result.Message);
        }

        [Fact]
        public async Task TransferAsync_ShouldFail_WhenInsufficientFunds()
        {
            // Arrange
            var sourceAccount = new BankAccount { AccountNumber = "A001", Balance = 150, Status = AccountStatus.Active };
            var destinationAccount = new BankAccount { AccountNumber = "A002", Balance = 300, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(sourceAccount);
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A002", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(destinationAccount);

            // Act
            var result = await _service.TransferAsync("A001", "A002", 60);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Insufficient funds. Minimum balance after transfer is 100.", result.Message);
        }

        [Fact]
        public async Task TransferAsync_ShouldFail_WhenDailyLimitExceeded()
        {
            // Arrange
            var sourceAccount = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };
            var destinationAccount = new BankAccount { AccountNumber = "A002", Balance = 300, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(sourceAccount);
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A002", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(destinationAccount);
            _mockTransactionRepo.Setup(x => x.GetTodayOutgoingTotalAsync("A001", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(450);

            // Act
            var result = await _service.TransferAsync("A001", "A002", 100);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Daily outgoing limit exceeded. Limit is 500.", result.Message);
        }

        [Fact]
        public async Task TransferAsync_ShouldSuccess_WhenInputIsValid()
        {
            // Arrange
            var sourceAccount = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };
            var destinationAccount = new BankAccount { AccountNumber = "A002", Balance = 300, Status = AccountStatus.Active };
            var dbTransaction = new Mock<IDbContextTransaction>();

            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(sourceAccount);
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A002", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(destinationAccount);
            _mockTransactionRepo.Setup(x => x.GetTodayOutgoingTotalAsync("A001", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(0);
            _mockUow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dbTransaction.Object);

            // Act
            var result = await _service.TransferAsync(" A001 ", " A002 ", 200);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Transfer completed.", result.Message);
            Assert.Equal(800, sourceAccount.Balance);
            Assert.Equal(500, destinationAccount.Balance);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.Is<Transaction>(t =>
                t.AccountNumber == "A001" &&
                t.Type == TransactionType.Transfer &&
                t.Amount == 200 &&
                t.Description.Contains("A002")), It.IsAny<CancellationToken>()), Times.Once);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.Is<Transaction>(t =>
                t.AccountNumber == "A002" &&
                t.Type == TransactionType.Transfer &&
                t.Amount == 200 &&
                t.Description.Contains("A001")), It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            dbTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TransferAsync_ShouldRollbackAndThrow_WhenExceptionOccurs()
        {
            // Arrange
            var sourceAccount = new BankAccount { AccountNumber = "A001", Balance = 1000, Status = AccountStatus.Active };
            var destinationAccount = new BankAccount { AccountNumber = "A002", Balance = 300, Status = AccountStatus.Active };
            var dbTransaction = new Mock<IDbContextTransaction>();

            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A001", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(sourceAccount);
            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync("A002", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(destinationAccount);
            _mockTransactionRepo.Setup(x => x.GetTodayOutgoingTotalAsync("A001", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(0);
            _mockUow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dbTransaction.Object);
            _mockUow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("save error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TransferAsync("A001", "A002", 200));
            dbTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            dbTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetHistoryAsync_ShouldTrimAccountNumberAndReturnData()
        {
            // Arrange
            var expected = new List<Transaction>
            {
                new() { AccountNumber = "A001", Type = TransactionType.Deposit, Amount = 100 }
            };

            _mockTransactionRepo.Setup(x => x.GetHistoryAsync("A001", TransactionType.Deposit, It.IsAny<CancellationToken>()))
                                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetHistoryAsync(" A001 ", TransactionType.Deposit);

            // Assert
            Assert.Single(result);
            Assert.Equal("A001", result[0].AccountNumber);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetHistoryAsync_ShouldPassNullAccountNumber_WhenInputIsEmpty(string? accountNumber)
        {
            // Arrange
            var expected = new List<Transaction>();
            _mockTransactionRepo.Setup(x => x.GetHistoryAsync(null, null, It.IsAny<CancellationToken>()))
                                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetHistoryAsync(accountNumber, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
