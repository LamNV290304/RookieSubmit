using Xunit;
using Moq;
using BankAccountSimulatorMVC.Interfaces;
using BankAccountSimulatorMVC.Services;
using Domain.Models;

namespace BankSimulator.Test
{
    public class BankSimulatorServiceTest
    {
        private readonly Mock<IBankAccountRepository> _mockRepo;
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly BankAccountService _service;

        public BankSimulatorServiceTest()
        {
            _mockRepo = new Mock<IBankAccountRepository>();
            _mockUow = new Mock<IUnitOfWork>();

            _mockUow.Setup(x => x.BankAccounts).Returns(_mockRepo.Object);

            _service = new BankAccountService(_mockUow.Object);
        }

        [Fact]
        public async Task GetAllAccounts_ShouldReturnEmptyList()
        {
            // Arrange
            _mockRepo.Setup(x => x.GetAllAsync())
                     .ReturnsAsync([]);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAccounts_ShouldReturnAccounts()
        {
            // Arrange
            var data = new List<BankAccount>
            {
                new() { AccountNumber = "1", Balance = 100 },
                new() { AccountNumber = "2", Balance = 200 }
            };

            _mockRepo.Setup(x => x.GetAllAsync())
                     .ReturnsAsync(data);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("1", result[0].AccountNumber);
        }

        [Fact]
        public async Task GetByAccountNumber_ShouldReturnNull()
        {
            // Arrange
            _mockRepo.Setup(x => x.GetByAccountNumberAsync(It.IsAny<string>()))
                     .ReturnsAsync((BankAccount?)null);

            // Act
            var result = await _service.GetByAccountNumberAsync("xxx");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByAccountNumber_ShouldReturnAccount()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = "123", Balance = 1000 };

            _mockRepo.Setup(x => x.GetByAccountNumberAsync("123"))
                     .ReturnsAsync(account);

            // Act
            var result = await _service.GetByAccountNumberAsync("123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("123", result!.AccountNumber);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_ShouldFail_WhenAccountNumberIsInvalid(string? accountNumber)
        {
            // Arrange
            var account = new BankAccount { AccountNumber = accountNumber! };

            // Act
            var result = await _service.CreateAsync(account);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account number is required.", result.Message);
            _mockRepo.Verify(x => x.AddAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenAccountNumberAlreadyExists()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = " 123 " };
            _mockRepo.Setup(x => x.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new BankAccount { AccountNumber = "123" });

            // Act
            var result = await _service.CreateAsync(account);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account number already exists.", result.Message);
            _mockRepo.Verify(x => x.AddAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldSuccess_WhenAccountNumberIsValidAndUnique()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = " 123 " };
            _mockRepo.Setup(x => x.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((BankAccount?)null);

            // Act
            var result = await _service.CreateAsync(account);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Account created successfully.", result.Message);
            Assert.Equal("123", account.AccountNumber);
            _mockRepo.Verify(x => x.AddAsync(account, It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task FreezeAsync_ShouldFail_WhenInvalidInput(string input)
        {
            // Arrange

            // Act
            var result = await _service.FreezeAsync(input);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account number is required.", result.Message);
        }

        [Fact]
        public async Task FreezeAsync_ShouldFail_WhenAccountNotFound()
        {
            // Arrange
            _mockRepo.Setup(x => x.GetByAccountNumberAsync("123"))
                     .ReturnsAsync((BankAccount?)null);

            // Act
            var result = await _service.FreezeAsync("123");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Account not found.", result.Message);
        }

        [Theory]
        [InlineData(AccountStatus.Frozen, "Account is already frozen.")]
        [InlineData(AccountStatus.Active, "Account is already active.")]
        public async Task UpdateStatus_ShouldFail_WhenAlreadyInStatus(
            AccountStatus currentStatus,
            string expectedMessage)
        {
            // Arrange
            var account = new BankAccount
            {
                AccountNumber = "123",
                Status = currentStatus
            };

            _mockRepo.Setup(x => x.GetByAccountNumberAsync("123"))
                     .ReturnsAsync(account);

            // Act
            var result = currentStatus == AccountStatus.Frozen
                ? await _service.FreezeAsync("123")
                : await _service.UnfreezeAsync("123");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(expectedMessage, result.Message);
        }

        [Theory]
        [InlineData(AccountStatus.Active, AccountStatus.Frozen)]
        [InlineData(AccountStatus.Frozen, AccountStatus.Active)]
        public async Task UpdateStatus_ShouldSuccess(
            AccountStatus currentStatus,
            AccountStatus expectedStatus)
        {
            // Arrange
            var account = new BankAccount
            {
                AccountNumber = "123",
                Status = currentStatus
            };

            _mockRepo.Setup(x => x.GetByAccountNumberAsync("123"))
                     .ReturnsAsync(account);

            // Act
            if (expectedStatus == AccountStatus.Frozen)
                await _service.FreezeAsync("123");
            else
                await _service.UnfreezeAsync("123");

            // Assert
            Assert.Equal(expectedStatus, account.Status);
            _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
    }
}