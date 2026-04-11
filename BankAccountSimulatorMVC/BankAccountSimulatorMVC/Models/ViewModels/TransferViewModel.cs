using System.ComponentModel.DataAnnotations;

namespace BankAccountSimulatorMVC.Models.ViewModels
{
    public class TransferViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Source account is required.")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination account is required.")]
        public string DestinationAccountNumber { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SourceAccountNumber.Equals(DestinationAccountNumber, StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "Source and destination account must be different.",
                    [nameof(SourceAccountNumber), nameof(DestinationAccountNumber)]);
            }
        }
    }
}
