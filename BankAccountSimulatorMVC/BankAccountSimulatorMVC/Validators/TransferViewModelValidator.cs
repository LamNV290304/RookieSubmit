using BankAccountSimulatorMVC.ViewModels;
using FluentValidation;

namespace BankAccountSimulatorMVC.Validators;

public class TransferViewModelValidator : AbstractValidator<TransferViewModel>
{
    public TransferViewModelValidator()
    {
        RuleFor(x => x.SourceAccountNumber)
            .NotEmpty().WithMessage("Source account is required.")
            .MaximumLength(32);

        RuleFor(x => x.DestinationAccountNumber)
            .NotEmpty().WithMessage("Destination account is required.")
            .MaximumLength(32)
            .Must((model, destination) => !string.Equals(model.SourceAccountNumber, destination, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source and destination account must be different.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");
    }
}