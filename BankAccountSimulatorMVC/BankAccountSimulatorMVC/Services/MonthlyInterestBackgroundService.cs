using BankAccountSimulatorMVC.Services.Interface;

namespace BankAccountSimulatorMVC.Services;

public class MonthlyInterestBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MonthlyInterestBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunMonthlyInterestAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTime.UtcNow);
            await Task.Delay(delay, stoppingToken);

            await RunMonthlyInterestAsync(stoppingToken);
        }
    }

    private async Task RunMonthlyInterestAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var monthlyInterestService = scope.ServiceProvider.GetRequiredService<IMonthlyInterestService>();

        try
        {
            var result = await monthlyInterestService.ApplyCurrentMonthAsync(cancellationToken);
            if (result.Success)
            {
                logger.LogInformation("MonthlyInterest: {Message}", result.Message);
            }
            else
            {
                logger.LogWarning("MonthlyInterest: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MonthlyInterest background run crashed.");
        }
    }

    private static TimeSpan GetDelayUntilNextRun(DateTime utcNow)
    {
        var nextRun = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 5, 0, DateTimeKind.Utc);
        if (utcNow >= nextRun)
        {
            nextRun = nextRun.AddMonths(1);
        }

        return nextRun - utcNow;
    }
}
