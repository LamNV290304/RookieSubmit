using System.ComponentModel.DataAnnotations;

namespace BankAccountSimulatorMVC.ViewModels
{
    public class TransferViewModel : IValidatableObject
    {
        private const string SourceAccountRequiredMessage = "Source account is required.";
        private const string DestinationAccountRequiredMessage = "Destination account is required.";
        private const string AmountGreaterThanZeroMessage = "Amount must be greater than 0.";
        private const string DifferentAccountsMessage = "Source and destination account must be different.";

        [Required(ErrorMessage = SourceAccountRequiredMessage)]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = DestinationAccountRequiredMessage)]
        public string DestinationAccountNumber { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = AmountGreaterThanZeroMessage)]
        public decimal Amount { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.Equals(SourceAccountNumber, DestinationAccountNumber, StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    DifferentAccountsMessage,
                    [nameof(SourceAccountNumber), nameof(DestinationAccountNumber)]);
            }
        }
    }
}
