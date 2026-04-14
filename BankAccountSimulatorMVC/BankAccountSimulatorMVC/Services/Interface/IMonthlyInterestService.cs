namespace BankAccountSimulatorMVC.Services.Interface;

public interface IMonthlyInterestService
{
    Task<ServiceResult> ApplyCurrentMonthAsync(CancellationToken cancellationToken = default);
}
