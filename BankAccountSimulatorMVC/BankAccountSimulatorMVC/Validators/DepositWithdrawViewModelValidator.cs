using BankAccountSimulatorMVC.ViewModels;
using FluentValidation;

namespace BankAccountSimulatorMVC.Validators;

public class DepositWithdrawViewModelValidator : AbstractValidator<DepositWithdrawViewModel>
{
    public DepositWithdrawViewModelValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required.")
            .MaximumLength(32);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");
    }
}