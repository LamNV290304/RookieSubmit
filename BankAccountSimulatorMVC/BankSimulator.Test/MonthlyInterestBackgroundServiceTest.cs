using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BankAccountSimulatorMVC.Services;
using BankAccountSimulatorMVC.Services.Interface;

namespace BankSimulator.Test
{
    public class MonthlyInterestBackgroundServiceTest
    {
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IMonthlyInterestService> _mockMonthlyInterestService;
        private readonly Mock<ILogger<MonthlyInterestBackgroundService>> _mockLogger;
        private readonly MonthlyInterestBackgroundService _service;

        public MonthlyInterestBackgroundServiceTest()
        {
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockMonthlyInterestService = new Mock<IMonthlyInterestService>();
            _mockLogger = new Mock<ILogger<MonthlyInterestBackgroundService>>();

            _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IMonthlyInterestService)))
                                .Returns(_mockMonthlyInterestService.Object);

            _service = new MonthlyInterestBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task RunMonthlyInterest_ShouldLogInformation_WhenServiceReturnsSuccess()
        {
            // Arrange
            _mockMonthlyInterestService.Setup(x => x.ApplyCurrentMonthAsync(It.IsAny<CancellationToken>()))
                                       .ReturnsAsync(ServiceResult.Ok("ok"));

            // Act
            await _service.InvokeRunMonthlyInterestAsync();

            // Assert
            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("MonthlyInterest: ok")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task RunMonthlyInterest_ShouldLogWarning_WhenServiceReturnsFail()
        {
            // Arrange
            _mockMonthlyInterestService.Setup(x => x.ApplyCurrentMonthAsync(It.IsAny<CancellationToken>()))
                                       .ReturnsAsync(ServiceResult.Fail("failed"));

            // Act
            await _service.InvokeRunMonthlyInterestAsync();

            // Assert
            _mockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("MonthlyInterest: failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task RunMonthlyInterest_ShouldLogError_WhenExceptionThrown()
        {
            // Arrange
            _mockMonthlyInterestService.Setup(x => x.ApplyCurrentMonthAsync(It.IsAny<CancellationToken>()))
                                       .ThrowsAsync(new InvalidOperationException("boom"));

            // Act
            await _service.InvokeRunMonthlyInterestAsync();

            // Assert
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("MonthlyInterest background run crashed.")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void GetDelayUntilNextRun_ShouldReturnPositiveDelayWithinCurrentMonth_WhenBeforeRunTime()
        {
            // Arrange
            var utcNow = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
            var method = typeof(MonthlyInterestBackgroundService)
                .GetMethod("GetDelayUntilNextRun", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            // Act
            var delay = (TimeSpan)method.Invoke(null, [utcNow])!;

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), delay);
        }

        [Fact]
        public void GetDelayUntilNextRun_ShouldMoveToNextMonth_WhenPastRunTime()
        {
            // Arrange
            var utcNow = new DateTime(2026, 5, 1, 0, 6, 0, DateTimeKind.Utc);
            var method = typeof(MonthlyInterestBackgroundService)
                .GetMethod("GetDelayUntilNextRun", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            // Act
            var delay = (TimeSpan)method.Invoke(null, [utcNow])!;

            // Assert
            var expected = new DateTime(2026, 6, 1, 0, 5, 0, DateTimeKind.Utc) - utcNow;
            Assert.Equal(expected, delay);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRunOnceAndStop_WhenCancellationRequested()
        {
            // Arrange
            _mockMonthlyInterestService.Setup(x => x.ApplyCurrentMonthAsync(It.IsAny<CancellationToken>()))
                                       .ReturnsAsync(ServiceResult.Ok("ok"));
            var testableService = new TestableMonthlyInterestBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await testableService.InvokeExecuteAsync(cts.Token);

            // Assert
            _mockMonthlyInterestService.Verify(x => x.ApplyCurrentMonthAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private sealed class TestableMonthlyInterestBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<MonthlyInterestBackgroundService> logger)
            : MonthlyInterestBackgroundService(scopeFactory, logger)
        {
            public Task InvokeExecuteAsync(CancellationToken cancellationToken)
                => base.ExecuteAsync(cancellationToken);
        }
    }
}
